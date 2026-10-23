using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Refit;
using SyZero.Application.Routing;
using SyZero.Application.Service;
using SyZero.Client.RemoteService;
using SyZero.Feign;
using SyZero.Feign.Proxy;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;
using SyZero.Serialization;
using SyZero.Service;
using Xunit;

namespace SyZero.Tests;

public class FeignTests
{
    [Fact]
    public async Task RequestFeignHandler_BuildsDynamicApiPath_WithServiceNamePrefix()
    {
        var innerHandler = new CaptureHandler();
        var handler = new RequestFeignHandler("sample-service", innerHandler);
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/orders/list");
        request.Options.Set(new HttpRequestOptionsKey<TypeInfo>(HttpRequestMessageOptions.InterfaceType), typeof(OrderClient).GetTypeInfo());

        await client.SendAsync(request);

        Assert.Equal("/api/sample-service/orders/orders/list", innerHandler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RequestFeignHandler_StripsApiPrefix_CaseInsensitively()
    {
        var innerHandler = new CaptureHandler();
        var handler = new RequestFeignHandler("sample-service", innerHandler);
        using var client = new HttpClient(handler);

        await client.GetAsync("http://localhost/api/orders/list");

        Assert.Equal("/orders/list", innerHandler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task RequestFeignHandler_NormalizesLegacyApiPrefixFromRoutingAttribute()
    {
        var innerHandler = new CaptureHandler();
        var handler = new RequestFeignHandler("sample-service", innerHandler);
        using var client = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/APIGetServiceInfo");
        request.Options.Set(new HttpRequestOptionsKey<TypeInfo>(HttpRequestMessageOptions.InterfaceType), typeof(SampleRemoteServiceClient).GetTypeInfo());

        await client.SendAsync(request);

        Assert.Equal("/api/sample-service/example2/GetServiceInfo", innerHandler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task AuthenticationFeignHandler_PreservesExistingAuthorizationHeader()
    {
        using var client = new HttpClient(new AuthenticationFeignHandler(new TestSySession("session-token"), new CaptureHandler()));
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", "external-token")
            }
        };

        await client.SendAsync(request);

        Assert.Equal("external-token", request.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task AuthenticationFeignHandler_SkipsBlankToken()
    {
        using var client = new HttpClient(new AuthenticationFeignHandler(new TestSySession(" "), new CaptureHandler()));
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/test");

        await client.SendAsync(request);

        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public async Task ResponseFeignHandler_UnwrapsSuccessfulEnvelope()
    {
        var handler = new ResponseFeignHandler("sample-service", new StaticResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":1,\"data\":{\"id\":7}}", System.Text.Encoding.UTF8, "application/json")
            }));
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://localhost/test");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"id\":7}", body);
    }

    [Fact]
    public async Task ResponseFeignHandler_ConvertsFailedEnvelopeToServerError()
    {
        var handler = new ResponseFeignHandler("sample-service", new StaticResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"code\":0,\"msg\":\"failed\"}", System.Text.Encoding.UTF8, "application/json")
            }));
        using var client = new HttpClient(handler);

        var response = await client.GetAsync("http://localhost/test");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public void RemoteServiceProxyFactoryManager_RegistersBuiltInFactories_AndAllowsOverrides()
    {
        // FeignRemoteServiceProvider 内部维护 manager 并预注册 HTTP / gRPC
        var provider = new FeignRemoteServiceProvider();
        var manager = (RemoteServiceProxyFactoryManager)typeof(FeignRemoteServiceProvider)
            .GetField("_factoryManager", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider)!;

        Assert.True(manager.IsProtocolRegistered(RemoteServiceProtocol.Http));
        Assert.True(manager.IsProtocolRegistered(RemoteServiceProtocol.Grpc));
        Assert.IsType<HttpProxyFactory>(manager.GetFactory(RemoteServiceProtocol.Http));
        Assert.IsType<GrpcProxyFactory>(manager.GetFactory(RemoteServiceProtocol.Grpc));

        var customFactory = new StubRemoteServiceProxyFactory(RemoteServiceProtocol.Http);
        provider.RegisterProxyFactory(customFactory);

        Assert.Same(customFactory, manager.GetFactory(RemoteServiceProtocol.Http));
    }

    [Fact]
    public void FeignRemoteServiceProvider_CreateEffectiveService_AppliesGlobalDefaults_WithoutMutatingOriginal()
    {
        var service = new RemoteServiceItem
        {
            ServiceName = "svc",
            AssemblyName = "Sample.Contracts"
        };
        var global = new RemoteServiceSetting
        {
            Protocol = RemoteServiceProtocol.Grpc,
            Strategy = "Random",
            Retry = 2,
            Timeout = 45,
            EnableSsl = true,
            MaxMessageSize = 4096
        };

        var result = (RemoteServiceItem)InvokePrivateStaticMethod(
            typeof(FeignRemoteServiceProvider),
            "CreateEffectiveService",
            service,
            global)!;

        Assert.Equal(RemoteServiceProtocol.Grpc, result.Protocol);
        Assert.Equal("Random", result.Strategy);
        Assert.Equal(2, result.Retry);
        Assert.Equal(45, result.Timeout);
        Assert.True(result.EnableSsl);
        Assert.Equal(4096, result.MaxMessageSize);

        Assert.Equal(RemoteServiceProtocol.Http, service.Protocol);
        Assert.Null(service.Strategy);
        Assert.Equal(0, service.Retry);
        Assert.Equal(30, service.Timeout);
        Assert.False(service.EnableSsl);
        Assert.Equal(0, service.MaxMessageSize);
    }

    [Fact]
    public void FeignRemoteServiceProvider_GetServiceEndpoint_UsesServiceInstanceAndRegistryProtocol()
    {
        var serviceManagement = new Mock<IServiceManagement>(MockBehavior.Strict);
        serviceManagement
            .Setup(x => x.GetServiceInstance("svc"))
            .ReturnsAsync(new ServiceInfo
            {
                ServiceName = "svc",
                ServiceAddress = "127.0.0.1",
                ServicePort = 8443,
                ServiceProtocol = ProtocolType.HTTPS
            });

        var endpoint = (string)InvokePrivateStaticMethod(
            typeof(FeignRemoteServiceProvider),
            "GetServiceEndpoint",
            serviceManagement.Object,
            new RemoteServiceItem
            {
                ServiceName = "svc",
                AssemblyName = "Sample.Contracts",
                Protocol = RemoteServiceProtocol.Http,
                EnableSsl = false
            })!;

        Assert.Equal("https://127.0.0.1:8443", endpoint);
        serviceManagement.Verify(x => x.GetServiceInstance("svc"), Times.Once);
        serviceManagement.Verify(x => x.GetService(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HttpProxyFactory_CreateRefitSettings_ReturnsExceptionsInsteadOfSwallowingFailures()
    {
        var method = typeof(HttpProxyFactory).GetMethod("CreateRefitSettings", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(HttpProxyFactory).FullName, "CreateRefitSettings");
        var settings = (RefitSettings)method.Invoke(new HttpProxyFactory(), [new DefaultJsonSerialize(), NullLogger.Instance])!;

        Assert.NotNull(settings.DeserializationExceptionFactory);
        var deserializationException = await settings.DeserializationExceptionFactory!(
            new HttpResponseMessage(HttpStatusCode.OK),
            new FormatException("bad json"));
        Assert.IsType<InvalidOperationException>(deserializationException);
        Assert.IsType<FormatException>(deserializationException!.InnerException);

        Assert.NotNull(settings.ExceptionFactory);
        var success = await settings.ExceptionFactory!(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"id\":1}", System.Text.Encoding.UTF8, "application/json")
            });
        Assert.Null(success);

        var clientError = await settings.ExceptionFactory!(
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"bad-request\"}", System.Text.Encoding.UTF8, "application/json")
            });
        Assert.IsType<HttpRequestException>(clientError);

        var serverError = await settings.ExceptionFactory!(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"code\":0,\"msg\":\"failed\"}", System.Text.Encoding.UTF8, "application/json")
            });
        Assert.IsType<SyMessageException>(serverError);
    }

    [Fact]
    public async Task RefitProxy_CanDeserializeUnwrappedServiceInfoDto()
    {
        var responseHandler = new ResponseFeignHandler("SyZero.Example2", new StaticResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"code\":1,\"data\":{\"serviceName\":\"SyZero.Example2\",\"version\":\"1.0.0\",\"port\":5003,\"timestamp\":\"2026-05-01 20:00:00\"}}",
                    System.Text.Encoding.UTF8,
                    "application/json")
            }));
        var requestHandler = new RequestFeignHandler("SyZero.Example2", responseHandler);
        var method = typeof(HttpProxyFactory).GetMethod("CreateRefitSettings", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(typeof(HttpProxyFactory).FullName, "CreateRefitSettings");
        var settings = (RefitSettings)method.Invoke(new HttpProxyFactory(), [new DefaultJsonSerialize(), NullLogger.Instance])!;
        using var client = new HttpClient(requestHandler)
        {
            BaseAddress = new Uri("http://localhost:5003")
        };

        var proxy = RestService.For<SampleRemoteServiceClient>(client, settings);
        var result = await proxy.GetServiceInfo();

        Assert.NotNull(result);
        Assert.Equal("SyZero.Example2", result.ServiceName);
        Assert.Equal("1.0.0", result.Version);
        Assert.Equal(5003, result.Port);
    }

    private static object? InvokePrivateStaticMethod(Type type, string methodName, params object[] parameters)
    {
        var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(type.FullName, methodName);
        return method.Invoke(null, parameters);
    }

    [Api("orders")]
    private interface OrderClient : IApplicationService
    {
    }

    [Api("example2")]
    public interface SampleRemoteServiceClient : IApplicationService
    {
        [Refit.Get("/GetServiceInfo")]
        Task<SampleServiceInfoDto> GetServiceInfo();
    }

    public sealed class SampleServiceInfoDto
    {
        public string? ServiceName { get; set; }
        public string? Version { get; set; }
        public int Port { get; set; }
        public string? Timestamp { get; set; }
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }

    private sealed class StaticResponseHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StaticResponseHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private sealed class StubRemoteServiceProxyFactory : IRemoteServiceProxyFactory
    {
        public StubRemoteServiceProxyFactory(RemoteServiceProtocol protocol)
        {
            Protocol = protocol;
        }

        public RemoteServiceProtocol Protocol { get; }

        public object CreateProxy(Type targetType, string endPoint, RemoteServiceItem service, IJsonSerialize jsonSerialize, IServiceProvider serviceProvider)
        {
            return new object();
        }
    }

    private sealed class TestSySession : ISySession
    {
        public TestSySession(string? token)
        {
            Token = token!;
        }

        public System.Security.Claims.ClaimsPrincipal Principal { get; } = new(new System.Security.Claims.ClaimsIdentity());

        public long? UserId => null;

        public string UserRole => string.Empty;

        public string UserName => string.Empty;

        public List<string> Permission => [];

        public string Token { get; }

        public ISySession Parse(System.Security.Claims.ClaimsPrincipal claimsPrincipal)
        {
            throw new NotSupportedException();
        }

        public ISySession Parse(string token)
        {
            throw new NotSupportedException();
        }
    }
}
