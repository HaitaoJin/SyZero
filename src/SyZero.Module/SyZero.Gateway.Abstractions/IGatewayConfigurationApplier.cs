using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关配置热下发抽象。具体网关实现负责把 <see cref="GatewayConfiguration"/>
    /// 推送到运行中的网关管线（例如 Ocelot 的 IFileConfigurationSetter、YARP 的 InMemoryConfigProvider）。
    /// </summary>
    public interface IGatewayConfigurationApplier
    {
        /// <summary>
        /// 将配置应用到运行中的网关。失败时实现应抛出异常或记录日志，由调用方决定后续策略。
        /// </summary>
        Task ApplyAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default);
    }
}
