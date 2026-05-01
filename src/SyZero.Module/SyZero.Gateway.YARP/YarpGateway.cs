using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Gateway;

namespace SyZero.Gateway.YARP
{
    /// <summary>
    /// 基于 YARP 内存提供者的 <see cref="IGateway"/> 实现：直接读取当前生效的
    /// <see cref="SyZeroYarpProxyConfigProvider"/>，把 RouteConfig 反向映射回 SyZero 中性 schema。
    /// </summary>
    /// <remarks>
    /// 注意：YARP RouteConfig 的丰富字段（Transforms / RateLimiterPolicy 等）会被简化丢弃；
    /// 反向映射主要用于 Admin 面板做"读取当前生效配置"的查询，不应作为持久化来源。
    /// </remarks>
    public sealed class YarpGateway : IGateway
    {
        private readonly SyZeroYarpProxyConfigProvider _provider;

        public YarpGateway(SyZeroYarpProxyConfigProvider provider)
        {
            _provider = provider;
        }

        public string ProviderName => "YARP";

        public Task<GatewayConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
        {
            var live = _provider.GetConfig();
            var dto = new GatewayConfiguration();
            foreach (var r in live.Routes)
            {
                dto.Routes.Add(new GatewayRoute
                {
                    UpstreamPathTemplate = r.Match?.Path ?? string.Empty,
                    UpstreamHttpMethod = r.Match?.Methods is null
                        ? new List<string>()
                        : new List<string>(r.Match.Methods),
                    ServiceName = string.IsNullOrEmpty(r.ClusterId) ? null : r.ClusterId
                });
            }
            return Task.FromResult(dto);
        }
    }
}
