using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Cache;

namespace SyZero
{
    /// <summary>
    /// 多级缓存与分布式缓存注册扩展。
    /// </summary>
    public static class SyZeroMultiLevelCacheExtension
    {
        /// <summary>
        /// 注册多级缓存（L1 内存 + L2 <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>）。
        /// 若未注册 <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>，则回退使用进程内内存实现，可在生产中替换为 SyZero.Redis。
        /// </summary>
        public static IServiceCollection AddSyZeroMultiLevelCache(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<MultiLevelCacheOptions>? configure = null,
            string sectionName = MultiLevelCacheOptions.SectionName)
        {
            services.AddOptions();
            services.AddMemoryCache();

            var options = new MultiLevelCacheOptions();
            configuration?.GetSection(sectionName)?.Bind(options);
            configure?.Invoke(options);

            services.Configure<MultiLevelCacheOptions>(o =>
            {
                o.L1ExpirationSeconds = options.L1ExpirationSeconds;
                o.EnableNullCache = options.EnableNullCache;
                o.NullCacheSeconds = options.NullCacheSeconds;
                o.JitterRatio = options.JitterRatio;
            });

            // 若调用方未注册 IDistributedCache，使用进程内实现兜底（开发场景）。
            services.AddDistributedMemoryCache();

            services.TryAddSingleton<IMultiLevelCache, MultiLevelCache>();
            return services;
        }
    }
}
