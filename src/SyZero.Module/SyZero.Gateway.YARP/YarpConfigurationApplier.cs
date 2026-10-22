using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SyZero.Gateway;

namespace SyZero.Gateway.YARP
{
    /// <summary>
    /// 把 <see cref="GatewayConfiguration"/> 热下发到 <see cref="SyZeroYarpProxyConfigProvider"/>，
    /// YARP 会通过 <c>IChangeToken</c> 自动感知并重建路由表，无需重启进程。
    /// </summary>
    public sealed class YarpConfigurationApplier : IGatewayConfigurationApplier
    {
        private readonly SyZeroYarpProxyConfigProvider _provider;
        private readonly ILogger<YarpConfigurationApplier> _logger;

        public YarpConfigurationApplier(
            SyZeroYarpProxyConfigProvider provider,
            ILogger<YarpConfigurationApplier> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        public Task ApplyAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            try
            {
                var (routes, clusters) = YarpConfigMapper.Map(configuration);
                _provider.Update(routes, clusters);
                _logger.LogInformation("已将 {RouteCount} 条路由 / {ClusterCount} 个集群热加载到 YARP。", routes.Count, clusters.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下发 YARP 配置失败。");
                throw;
            }
            return Task.CompletedTask;
        }
    }
}
