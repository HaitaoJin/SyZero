using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using SyZero.Runtime.Security;
using SyZero.Service;
using OcelotService = Ocelot.Values.Service;

namespace SyZero.Gateway.Ocelot
{
    /// <summary>
    /// 基于 SyZero <see cref="IServiceManagement"/> 抽象的 Ocelot 服务发现提供者。
    /// 与 <see cref="SyZeroConsulServiceBuilder"/> 不同，本实现完全脱离具体后端，
    /// 凡是注册了 <c>IServiceManagement</c>（Consul / Nacos / Redis / Local / DB）即可直接使用。
    /// 通过 <c>ocelot.json</c> 中
    /// <c>GlobalConfiguration.ServiceDiscoveryProvider.Type = "SyZeroDiscovery"</c> 启用
    /// （Ocelot 23 工厂以 provider 类的 <c>Type.Name</c> 与 <c>Type</c> 字段做大小写不敏感比较）。
    /// </summary>
    public sealed class SyZeroDiscovery : IServiceDiscoveryProvider
    {
        /// <summary>
        /// 在 <c>ocelot.json</c> 中识别此 Provider 的 <c>Type</c> 名称。
        /// 必须与本类的简单类型名一致（Ocelot 23 工厂硬编码做名称匹配）。
        /// </summary>
        public const string ProviderType = nameof(SyZeroDiscovery);

        private readonly IServiceManagement _serviceManagement;
        private readonly string _serviceName;
        private readonly ILogger<SyZeroDiscovery>? _logger;

        public SyZeroDiscovery(
            IServiceManagement serviceManagement,
            string serviceName,
            ILogger<SyZeroDiscovery>? logger = null)
        {
            _serviceManagement = serviceManagement;
            _serviceName = serviceName;
            _logger = logger;
        }

        public async Task<List<OcelotService>> GetAsync()
        {
            if (string.IsNullOrWhiteSpace(_serviceName))
            {
                return new List<OcelotService>();
            }

            List<ServiceInfo>? instances;
            try
            {
                instances = await _serviceManagement.GetHealthyServices(_serviceName).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                _logger?.LogWarning(ex, "SyZero.Gateway.Ocelot：解析服务实例失败 {ServiceName}", _serviceName);
                return new List<OcelotService>();
            }

            if (instances is null || instances.Count == 0)
            {
                return new List<OcelotService>();
            }

            return instances
                .Where(i => !string.IsNullOrWhiteSpace(i.ServiceAddress) && i.ServicePort > 0)
                .Select(i => new OcelotService(
                    name: i.ServiceName ?? _serviceName,
                    hostAndPort: new ServiceHostAndPort(i.ServiceAddress, i.ServicePort, MapScheme(i.ServiceProtocol)),
                    id: i.ServiceID ?? string.Empty,
                    version: i.Version ?? string.Empty,
                    tags: i.Tags ?? new List<string>()))
                .ToList();
        }

        private static string MapScheme(ProtocolType protocol)
        {
            return protocol switch
            {
                ProtocolType.HTTPS => "https",
                ProtocolType.GRPC => "http",
                _ => "http"
            };
        }
    }
}
