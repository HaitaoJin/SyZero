using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using SyZero.Application.Service;
using SyZero.Client;
using SyZero.Client.RemoteService;
using SyZero.Feign.Proxy;
using SyZero.Runtime.Security;
using SyZero.Serialization;
using SyZero.Service;

namespace SyZero.Feign
{
    /// <summary>
    /// Feign 远程服务 Provider
    /// 扫描 <see cref="IApplicationService"/> 接口（无本地实现的）并按协议生成代理。
    /// </summary>
    public class FeignRemoteServiceProvider : IRemoteServiceProvider
    {
        private readonly RemoteServiceProxyFactoryManager _factoryManager;

        /// <summary>
        /// 初始化 Feign Provider，并注册内置的 HTTP / gRPC 代理工厂
        /// </summary>
        public FeignRemoteServiceProvider()
        {
            _factoryManager = new RemoteServiceProxyFactoryManager();
            _factoryManager.RegisterFactory(new HttpProxyFactory());
            _factoryManager.RegisterFactory(new GrpcProxyFactory());
        }

        /// <inheritdoc />
        public string Name => "Feign";

        /// <summary>
        /// 注册（或覆盖）协议级代理工厂，可在调用 <see cref="IServiceCollection"/> 注册前扩展
        /// </summary>
        public void RegisterProxyFactory(IRemoteServiceProxyFactory factory)
        {
            _factoryManager.RegisterFactory(factory);
        }

        /// <inheritdoc />
        public void Register(IServiceCollection services, RemoteServiceOptions options)
        {
            var definedTypes = ReflectionHelper.GetTypes();

            var baseFallback = typeof(IFallback);
            var baseType = typeof(IApplicationService);
            var types = definedTypes.Where(type => baseType.IsAssignableFrom(type) && type != baseType);
            var interfaceTypeInfos = types.Where(t => t.IsInterface);

            var implTypeInfos = types.Where(t => t.IsClass && !t.IsAbstract && !baseFallback.IsAssignableFrom(t) && t.IsVisible);

            var feignInterfaces = interfaceTypeInfos.Where(p => !p.IsGenericType
                && !implTypeInfos.Any(t => p.IsAssignableFrom(t))
                && baseType.IsAssignableFrom(p));

            foreach (var targetType in feignInterfaces)
            {
                RegisterFeignProxy(services, targetType, options);
            }
        }

        /// <summary>
        /// 注册单个 Feign 代理
        /// </summary>
        private void RegisterFeignProxy(IServiceCollection services, Type targetType, RemoteServiceOptions options)
        {
            services.AddScoped(targetType, sp =>
            {
                var jsonSerialize = sp.GetRequiredService<IJsonSerialize>();
                var serviceManagement = sp.GetRequiredService<IServiceManagement>();

                var assemblyName = targetType.Assembly.GetName().Name;
                var item = options.Service.FirstOrDefault(
                    p => string.Equals(p.AssemblyName, assemblyName, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    throw new Exception($"Assembly:{assemblyName} 未在 RemoteService 配置中注册!");
                }

                var effectiveItem = CreateEffectiveService(item, options.Global);

                var endPoint = GetServiceEndpoint(serviceManagement, effectiveItem);

                return _factoryManager.CreateProxy(targetType, endPoint, effectiveItem, jsonSerialize, sp);
            });
        }

        /// <summary>
        /// 合并全局配置（服务配置优先）
        /// </summary>
        private static RemoteServiceItem CreateEffectiveService(RemoteServiceItem service, RemoteServiceSetting global)
        {
            var effective = new RemoteServiceItem
            {
                ServiceName = service.ServiceName,
                AssemblyName = service.AssemblyName,
                Protocol = service.Protocol,
                Strategy = service.Strategy,
                Retry = service.Retry,
                Timeout = service.Timeout,
                EnableSsl = service.EnableSsl,
                MaxMessageSize = service.MaxMessageSize
            };

            MergeGlobalSettings(effective, global);
            return effective;
        }

        /// <summary>
        /// 合并全局配置（服务配置优先）
        /// </summary>
        private static void MergeGlobalSettings(RemoteServiceItem service, RemoteServiceSetting global)
        {
            if (global == null) return;

            if (service.Protocol == RemoteServiceProtocol.Http && global.Protocol != RemoteServiceProtocol.Http)
            {
                service.Protocol = global.Protocol;
            }

            if (string.IsNullOrEmpty(service.Strategy) && !string.IsNullOrEmpty(global.Strategy))
            {
                service.Strategy = global.Strategy;
            }

            // 合并超时配置（默认值为 30，如果未修改则使用全局配置）
            if (service.Timeout == 30 && global.Timeout != 30)
            {
                service.Timeout = global.Timeout;
            }

            if (service.MaxMessageSize == 0 && global.MaxMessageSize > 0)
            {
                service.MaxMessageSize = global.MaxMessageSize;
            }

            if (service.Retry == 0 && global.Retry > 0)
            {
                service.Retry = global.Retry;
            }

            if (!service.EnableSsl && global.EnableSsl)
            {
                service.EnableSsl = true;
            }
        }

        /// <summary>
        /// 解析服务端点
        /// </summary>
        private static string GetServiceEndpoint(IServiceManagement serviceManagement, RemoteServiceItem service)
        {
            var serviceInstance = serviceManagement.GetServiceInstance(service.ServiceName)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            if (serviceInstance == null)
            {
                throw new Exception($"未找到服务: {service.ServiceName}");
            }

            var scheme = ResolveEndpointScheme(serviceInstance, service);
            return $"{scheme}://{serviceInstance.ServiceAddress}:{serviceInstance.ServicePort}";
        }

        private static string ResolveEndpointScheme(ServiceInfo serviceInstance, RemoteServiceItem service)
        {
            if (serviceInstance.ServiceProtocol == ProtocolType.HTTPS)
            {
                return "https";
            }

            if (serviceInstance.ServiceProtocol == ProtocolType.HTTP)
            {
                return service.EnableSsl ? "https" : "http";
            }

            return service.EnableSsl ? "https" : "http";
        }
    }
}
