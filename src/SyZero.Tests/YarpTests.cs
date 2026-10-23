using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SyZero.Gateway;
using SyZero.Runtime.Security;
using SyZero.Service;
using SyZero.Gateway.YARP;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace SyZero.Tests;

public class YarpTests
{
    [Fact]
    public void AddSyZeroYarp_RegistersGatewayAbstractions_AndYarpProxyProvider()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("https://app.example.com");
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IWebHostEnvironment>(CreateEnv());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddSyZeroYarp(opt => opt.LoadFromConfigurationFile = false, configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SyZeroYarpOptions>();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policy = corsOptions.Value.GetPolicy(options.CorsPolicyName);

        Assert.Equal("YARP", provider.GetRequiredService<IGateway>().ProviderName);
        Assert.NotNull(provider.GetService<IGatewayConfigurationApplier>());
        Assert.NotNull(provider.GetService<IGatewayConfigurationStore>());
        Assert.NotNull(provider.GetService<SyZeroYarpProxyConfigProvider>());
        Assert.NotNull(provider.GetService<IProxyConfigProvider>());
        Assert.NotNull(policy);
        Assert.Equal(new[] { "https://app.example.com" }, options.AllowedOrigins);
    }

    [Fact]
    public async Task YarpConfigurationApplier_PushesRoutesAndClustersIntoProvider()
    {
        var providerImpl = new SyZeroYarpProxyConfigProvider();
        var applier = new YarpConfigurationApplier(providerImpl, NullLogger<YarpConfigurationApplier>.Instance);

        var dto = new GatewayConfiguration();
        dto.Routes.Add(new GatewayRoute
        {
            UpstreamPathTemplate = "/user-service/{everything}",
            UpstreamHttpMethod = new List<string> { "GET", "POST" },
            DownstreamPathTemplate = "/api/{everything}",
            DownstreamScheme = "http",
            ServiceName = "user-service"
        });

        await applier.ApplyAsync(dto);
        var live = providerImpl.GetConfig();

        Assert.Single(live.Routes);
        Assert.Single(live.Clusters);
        Assert.Equal("user-service", live.Routes[0].ClusterId);
        Assert.Equal("/user-service/{**catchAll}", live.Routes[0].Match!.Path);
        Assert.Contains("GET", live.Routes[0].Match!.Methods!);
        // ServiceName 不为空时，cluster 应当生成 discovery:// 占位 destination
        Assert.Equal("discovery://user-service/", live.Clusters[0].Destinations!["d_discovery"].Address);
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolver_ExpandsDiscoveryUriIntoHealthyInstances()
    {
        var sm = new Mock<IServiceManagement>();
        sm.Setup(x => x.GetHealthyServices("order-service")).ReturnsAsync(new List<ServiceInfo>
        {
            new() { ServiceName = "order-service", ServiceAddress = "10.0.0.1", ServicePort = 5001, ServiceProtocol = ProtocolType.HTTP },
            new() { ServiceName = "order-service", ServiceAddress = "10.0.0.2", ServicePort = 5001, ServiceProtocol = ProtocolType.HTTPS }
        });

        var resolver = new SyZeroYarpServiceDiscoveryDestinationResolver(
            sm.Object, NullLogger<SyZeroYarpServiceDiscoveryDestinationResolver>.Instance);

        var input = new Dictionary<string, DestinationConfig>
        {
            ["d_discovery"] = new() { Address = "discovery://order-service/" },
            ["d_static"] = new() { Address = "http://other-host:9000/" }
        };

        var result = await resolver.ResolveDestinationsAsync(input, CancellationToken.None);

        // 静态目标透传
        Assert.Equal("http://other-host:9000/", result.Destinations["d_static"].Address);
        // discovery 占位被展开为两个真实 destination
        Assert.Equal("http://10.0.0.1:5001/", result.Destinations["d_discovery_0"].Address);
        Assert.Equal("https://10.0.0.2:5001/", result.Destinations["d_discovery_1"].Address);
    }

    [Fact]
    public async Task ServiceDiscoveryDestinationResolver_FiresChangeToken_WhenServiceManagementSubscriberSignals()
    {
        Action<List<ServiceInfo>>? capturedCallback = null;
        var sm = new Mock<IServiceManagement>();
        sm.Setup(x => x.GetHealthyServices("pay-service")).ReturnsAsync(new List<ServiceInfo>
        {
            new() { ServiceName = "pay-service", ServiceAddress = "10.0.0.9", ServicePort = 9000, ServiceProtocol = ProtocolType.HTTP }
        });
        sm.Setup(x => x.Subscribe("pay-service", It.IsAny<Action<List<ServiceInfo>>>()))
          .Callback<string, Action<List<ServiceInfo>>>((_, cb) => capturedCallback = cb)
          .Returns(Task.CompletedTask);

        using var resolver = new SyZeroYarpServiceDiscoveryDestinationResolver(
            sm.Object, NullLogger<SyZeroYarpServiceDiscoveryDestinationResolver>.Instance);

        var input = new Dictionary<string, DestinationConfig>
        {
            ["d_discovery"] = new() { Address = "discovery://pay-service/" }
        };

        var first = await resolver.ResolveDestinationsAsync(input, CancellationToken.None);
        Assert.NotNull(first.ChangeToken);
        Assert.False(first.ChangeToken!.HasChanged);

        // 模拟 Consul/Nacos 的服务实例上下线回调
        Assert.NotNull(capturedCallback);
        capturedCallback!(new List<ServiceInfo>());

        // ChangeToken 应当被触发，YARP 据此重新调用 ResolveDestinationsAsync
        Assert.True(first.ChangeToken!.HasChanged);
        sm.Verify(x => x.Subscribe("pay-service", It.IsAny<Action<List<ServiceInfo>>>()), Times.Once);
    }

    [Theory]
    [InlineData(GatewayLoadBalancerOptions.RoundRobin, "RoundRobin")]
    [InlineData(GatewayLoadBalancerOptions.LeastConnection, "LeastRequests")]
    [InlineData(GatewayLoadBalancerOptions.NoLoadBalance, "FirstAlphabetical")]
    public async Task YarpConfigurationApplier_MapsLoadBalancingPolicy(string syzeroLb, string expectedYarpPolicy)
    {
        var providerImpl = new SyZeroYarpProxyConfigProvider();
        var applier = new YarpConfigurationApplier(providerImpl, NullLogger<YarpConfigurationApplier>.Instance);

        var dto = new GatewayConfiguration();
        dto.Routes.Add(new GatewayRoute
        {
            UpstreamPathTemplate = "/svc/{everything}",
            DownstreamPathTemplate = "/api/{everything}",
            ServiceName = "svc",
            LoadBalancerOptions = new GatewayLoadBalancerOptions { Type = syzeroLb }
        });

        await applier.ApplyAsync(dto);
        var live = providerImpl.GetConfig();

        Assert.Equal(expectedYarpPolicy, live.Clusters[0].LoadBalancingPolicy);
    }

    private static IConfiguration CreateConfiguration(string? origins)
    {
        var values = new Dictionary<string, string?>
        {
            ["SyZero:Name"] = "SyZero.Gateway"
        };
        if (origins != null) values["SyZero:CorsOrigins"] = origins;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IWebHostEnvironment CreateEnv()
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.SetupGet(e => e.ContentRootPath).Returns(Path.GetTempPath());
        mock.SetupGet(e => e.EnvironmentName).Returns("Test");
        return mock.Object;
    }
}
