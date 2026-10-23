using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Ocelot.ServiceDiscovery;
using SyZero.Gateway.Ocelot;
using SyZero.Runtime.Security;
using SyZero.Service;
using Xunit;

namespace SyZero.Tests;

public class OcelotTests
{
    [Fact]
    public void AddSyZeroOcelot_RegistersGatewayServices_AndBindsConfiguredCorsOrigins()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration("https://app.example.com, https://admin.example.com");

        services.AddSyZeroOcelot(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<SyZeroOcelotOptions>();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policy = corsOptions.Value.GetPolicy(options.CorsPolicyName);

        Assert.NotNull(provider.GetService<IHttpContextAccessor>());
        Assert.NotNull(policy);
        Assert.Equal(new[] { "https://app.example.com", "https://admin.example.com" }, options.AllowedOrigins);
        Assert.Equal(options.AllowedOrigins, policy!.Origins);
        Assert.True(policy.SupportsCredentials);
    }

    [Fact]
    public void AddSyZeroOcelot_WithoutConfiguredOrigins_FallsBackToWildcardCors()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(null);

        services.AddSyZeroOcelot(options =>
        {
            options.EnableSwagger = false;
        }, configuration);

        using var provider = services.BuildServiceProvider();
        var gatewayOptions = provider.GetRequiredService<SyZeroOcelotOptions>();
        var corsOptions = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policy = corsOptions.Value.GetPolicy(gatewayOptions.CorsPolicyName);

        Assert.NotNull(policy);
        Assert.Empty(gatewayOptions.AllowedOrigins);
        Assert.Contains(policy!.Origins, origin => origin == CorsConstants.AnyOrigin);
        Assert.False(policy.SupportsCredentials);
    }

    private static IConfiguration CreateConfiguration(string? origins)
    {
        var values = new Dictionary<string, string?>
        {
            ["SyZero:Name"] = "SyZero.Gateway",
            ["GlobalConfiguration:ServiceDiscoveryProvider:Host"] = "127.0.0.1",
            ["GlobalConfiguration:ServiceDiscoveryProvider:Port"] = "8500",
            ["GlobalConfiguration:ServiceDiscoveryProvider:Type"] = "Consul",
            ["GlobalConfiguration:ServiceDiscoveryProvider:ConfigurationKey"] = "SyZero.Gateway"
        };

        if (origins != null)
        {
            values["SyZero:CorsOrigins"] = origins;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    [Fact]
    public async Task SyZeroDiscovery_ResolvesHealthyInstancesViaIServiceManagement()
    {
        var sm = new Mock<IServiceManagement>();
        sm.Setup(x => x.GetHealthyServices("billing-service")).ReturnsAsync(new List<SyZero.Service.ServiceInfo>
        {
            new() { ServiceName = "billing-service", ServiceID = "n1", ServiceAddress = "10.0.0.1", ServicePort = 5000, ServiceProtocol = ProtocolType.HTTP, Version = "1.0" },
            new() { ServiceName = "billing-service", ServiceID = "n2", ServiceAddress = "10.0.0.2", ServicePort = 5000, ServiceProtocol = ProtocolType.HTTPS, Version = "1.0" }
        });

        var provider = new SyZeroDiscovery(sm.Object, "billing-service");
        var services = await provider.GetAsync();

        Assert.Equal(2, services.Count);
        Assert.Equal("10.0.0.1", services[0].HostAndPort.DownstreamHost);
        Assert.Equal(5000, services[0].HostAndPort.DownstreamPort);
        Assert.Equal("http", services[0].HostAndPort.Scheme);
        Assert.Equal("https", services[1].HostAndPort.Scheme);
        // ProviderType 必须等于类的简单名（Ocelot 23 通过 GetType().Name 与 config.Type 对比）
        Assert.Equal(nameof(SyZeroDiscovery), SyZeroDiscovery.ProviderType);
        Assert.Equal(nameof(SyZeroDiscovery), provider.GetType().Name);
    }

    [Fact]
    public void AddSyZeroOcelot_WithEnableSyZeroServiceDiscovery_RegistersFinderDelegate()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(null);

        services.AddSingleton<IServiceManagement>(new Mock<IServiceManagement>().Object);
        services.AddSyZeroOcelot(opts =>
        {
            opts.EnableSyZeroServiceDiscovery = true;
            opts.EnableSwagger = false;
        }, configuration);

        Assert.Contains(services, d => d.ServiceType == typeof(ServiceDiscoveryFinderDelegate));
    }
}
