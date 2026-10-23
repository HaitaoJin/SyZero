using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SyZero.BackgroundJobs
{
    /// <summary>SyZero.BackgroundJobs 注册扩展。</summary>
    public static class SyZeroBackgroundJobsExtension
    {
        /// <summary>
        /// 注册 Channel 默认实现 + Worker（IHostedService）。业务侧需自行用
        /// <see cref="AddBackgroundJob{TJob}"/> 把每种 Job 注册到 DI（默认 Scoped）。
        /// </summary>
        public static IServiceCollection AddSyZeroBackgroundJobs(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<BackgroundJobsOptions>? configure = null)
        {
            var section = configuration?.GetSection("BackgroundJobs");
            if (section != null) services.Configure<BackgroundJobsOptions>(section);
            if (configure != null) services.Configure(configure);

            services.AddSingleton<BackgroundJobChannel>();
            services.AddSingleton<IBackgroundJobStateStore, MemoryBackgroundJobStateStore>();
            services.AddSingleton<IBackgroundJobManager, ChannelBackgroundJobManager>();
            services.AddHostedService<BackgroundJobWorker>();
            return services;
        }

        /// <summary>把一种 Job 注册到 DI（Scoped）。</summary>
        public static IServiceCollection AddBackgroundJob<TJob>(this IServiceCollection services)
            where TJob : class
        {
            services.AddScoped<TJob>();
            return services;
        }
    }
}
