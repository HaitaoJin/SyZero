using Microsoft.Extensions.DependencyInjection;

namespace SyZero.DistributedLock
{
    /// <summary>SyZero.DistributedLock 注册扩展。</summary>
    public static class SyZeroDistributedLockExtension
    {
        /// <summary>
        /// 注册 <see cref="LocalDistributedLockProvider"/> 为默认实现。
        /// 多实例部署请改注册基于 Redis / DB 的实现（覆盖即可）。
        /// </summary>
        public static IServiceCollection AddSyZeroDistributedLock(this IServiceCollection services)
        {
            services.AddSingleton<IDistributedLockProvider, LocalDistributedLockProvider>();
            return services;
        }
    }
}
