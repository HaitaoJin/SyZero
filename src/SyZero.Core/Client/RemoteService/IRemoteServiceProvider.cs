using Microsoft.Extensions.DependencyInjection;

namespace SyZero.Client.RemoteService
{
    /// <summary>
    /// 远程服务 Provider（服务级抽象）
    /// 每种远程调用方案（Feign、WebApiClient、自研 RPC 等）实现一个 Provider，
    /// 负责把扫描到的接口注册成可注入的代理。
    /// </summary>
    public interface IRemoteServiceProvider
    {
        /// <summary>
        /// Provider 名称（用于诊断/日志）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 把符合条件的接口代理注册到 DI 容器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="options">远程服务配置</param>
        void Register(IServiceCollection services, RemoteServiceOptions options);
    }
}
