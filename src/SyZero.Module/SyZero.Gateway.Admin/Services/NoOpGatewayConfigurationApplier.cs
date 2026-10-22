using System.Threading;
using System.Threading.Tasks;
using SyZero.Gateway;

namespace SyZero.Gateway.Admin.Services
{
    /// <summary>
    /// 默认的空 Applier 实现：当主机未注册具体网关后端的 <see cref="IGatewayConfigurationApplier"/> 时使用。
    /// 配置写盘后不再触发任何运行时下发；变更通常需要重启网关进程才能生效。
    /// </summary>
    public sealed class NoOpGatewayConfigurationApplier : IGatewayConfigurationApplier
    {
        public Task ApplyAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
