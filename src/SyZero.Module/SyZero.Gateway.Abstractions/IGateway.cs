using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关抽象。具体网关后端（如 Ocelot、YARP）通过实现该接口接入 SyZero。
    /// </summary>
    public interface IGateway
    {
        /// <summary>
        /// 当前实现的标识，例如 "Ocelot"、"YARP"。供诊断与日志使用。
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 读取当前生效的网关配置（路由 / Swagger 端点等）。
        /// </summary>
        Task<GatewayConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
    }
}
