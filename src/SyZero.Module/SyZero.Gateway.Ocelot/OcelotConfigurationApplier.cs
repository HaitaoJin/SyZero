using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Setter;
using SyZero.Gateway;

namespace SyZero.Gateway.Ocelot
{
    /// <summary>
    /// 把 <see cref="GatewayConfiguration"/> 热下发到 Ocelot 的 <see cref="IFileConfigurationSetter"/>。
    /// </summary>
    public sealed class OcelotConfigurationApplier : IGatewayConfigurationApplier
    {
        private readonly IFileConfigurationSetter _setter;
        private readonly ILogger<OcelotConfigurationApplier> _logger;

        public OcelotConfigurationApplier(
            IFileConfigurationSetter setter,
            ILogger<OcelotConfigurationApplier> logger)
        {
            _setter = setter;
            _logger = logger;
        }

        public async Task ApplyAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (configuration is null) throw new ArgumentNullException(nameof(configuration));

            try
            {
                var fileConfig = MapToFileConfiguration(configuration);
                await _setter.Set(fileConfig).ConfigureAwait(false);
                _logger.LogInformation("已将 {Count} 条路由热加载到 Ocelot。", fileConfig.Routes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下发 Ocelot 配置失败，路由变更可能需要重启进程才能生效。");
                throw;
            }
        }

        internal static FileConfiguration MapToFileConfiguration(GatewayConfiguration dto)
        {
            var file = new FileConfiguration();
            foreach (var r in dto.Routes)
            {
                if (r.Disabled)
                {
                    // Disabled 路由仅保留在 configuration.json 中以便 Admin 面板编辑，不下发到 Ocelot。
                    continue;
                }
                file.Routes.Add(new FileRoute
                {
                    UpstreamPathTemplate = r.UpstreamPathTemplate,
                    UpstreamHttpMethod = r.UpstreamHttpMethod ?? new List<string>(),
                    DownstreamPathTemplate = r.DownstreamPathTemplate,
                    DownstreamScheme = r.DownstreamScheme,
                    ServiceName = r.ServiceName ?? string.Empty,
                    LoadBalancerOptions = new FileLoadBalancerOptions
                    {
                        Type = r.LoadBalancerOptions?.Type ?? GatewayLoadBalancerOptions.RoundRobin
                    }
                });
            }
            return file;
        }
    }
}
