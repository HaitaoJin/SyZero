using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using SyZero.Client.RemoteService;
using SyZero.Serialization;
using SyZero.Util;
using SyZero.Web.Common;

namespace SyZero.Feign.Proxy
{
    /// <summary>
    /// HTTP 协议代理工厂（基于 Refit）
    /// </summary>
    public class HttpProxyFactory : IRemoteServiceProxyFactory
    {
        /// <summary>
        /// 支持的协议类型
        /// </summary>
        public RemoteServiceProtocol Protocol => RemoteServiceProtocol.Http;

        /// <summary>
        /// 解析 ILogger，DI 未就绪时回退到 NullLogger，避免在工厂初始化阶段抛异常。
        /// </summary>
        private static ILogger ResolveLogger()
        {
            try
            {
                return SyZeroUtil.GetService<ILogger<HttpProxyFactory>>() ?? (ILogger)NullLogger<HttpProxyFactory>.Instance;
            }
            catch
            {
                return NullLogger<HttpProxyFactory>.Instance;
            }
        }

        /// <summary>
        /// 创建 HTTP 代理实例
        /// </summary>
        public object CreateProxy(Type targetType, string endPoint, RemoteServiceItem service, IJsonSerialize jsonSerialize)
        {
            try
            {
                var httpClient = CreateHttpClient(endPoint, service);
                var settings = CreateRefitSettings(jsonSerialize);
                return RestService.For(targetType, httpClient, settings);
            }
            catch (Exception ex)
            {
                ResolveLogger().LogError(ex, "HTTP 代理创建失败: EndPoint({EndPoint}) ServiceName({ServiceName})", endPoint, service?.ServiceName);
                throw;
            }
        }

        /// <summary>
        /// 创建 HttpClient
        /// </summary>
        private HttpClient CreateHttpClient(string endPoint, RemoteServiceItem service)
        {
            var handler = CreateHttpMessageHandler(service);
            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri(endPoint),
                Timeout = TimeSpan.FromSeconds(service.Timeout)
            };
            return httpClient;
        }

        /// <summary>
        /// 创建 Refit 设置
        /// </summary>
        private RefitSettings CreateRefitSettings(IJsonSerialize jsonSerialize)
        {
            return new RefitSettings
            {
                DeserializationExceptionFactory = (httpResponse, exception) =>
                {
                    ResolveLogger().LogError(exception, "Feign 响应反序列化失败");
                    return Task.FromResult<Exception>(new InvalidOperationException("Feign 响应反序列化失败。", exception));
                },
                ContentSerializer = new NewtonsoftJsonContentSerializer(
                    new JsonSerializerSettings
                    {
                        Converters = { new LongToStrConverter() }
                    }
                ),
                ExceptionFactory = async (httpResponse) =>
                {
                    var jsonString = httpResponse.Content == null
                        ? string.Empty
                        : await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    {
                        var data = jsonSerialize.JSONToObject<SyMessageBoxModel>(jsonString);
                        if (data != null)
                        {
                            return new SyMessageException(data);
                        }
                    }

                    return new HttpRequestException(
                        $"Feign 请求失败，状态码: {(int)httpResponse.StatusCode} ({httpResponse.StatusCode})。",
                        null,
                        httpResponse.StatusCode);
                }
            };
        }

        /// <summary>
        /// 创建 HTTP 消息处理器链
        /// </summary>
        private HttpMessageHandler CreateHttpMessageHandler(RemoteServiceItem service)
        {
            var responseHandler = new ResponseFeignHandler(service.ServiceName)
            {
                InnerHandler = new HttpClientHandler()
            };
            var authenticationHandler = new AuthenticationFeignHandler(service.ServiceName, responseHandler);
            var requestHandler = new RequestFeignHandler(service.ServiceName, authenticationHandler);
            return requestHandler;
        }
    }
}
