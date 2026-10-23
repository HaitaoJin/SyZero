using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.DistributedTransaction;

namespace SyZero
{
    /// <summary>
    /// SyZero.DistributedTransaction 注册扩展。
    /// </summary>
    public static class SyZeroDistributedTransactionExtension
    {
        /// <summary>
        /// 注册 Outbox 投递（默认内存存储 + 后台 BackgroundService 投递到 IEventBus）。
        /// 业务可注册自己的 IOutboxStore 替换默认实现。
        /// </summary>
        public static IServiceCollection AddSyZeroOutbox(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<OutboxOptions>? configure = null,
            string sectionName = OutboxOptions.SectionName)
        {
            ArgumentNullException.ThrowIfNull(services);
            var options = new OutboxOptions();
            configuration?.GetSection(sectionName)?.Bind(options);
            configure?.Invoke(options);
            services.Configure<OutboxOptions>(o =>
            {
                o.PollIntervalSeconds = options.PollIntervalSeconds;
                o.BatchSize = options.BatchSize;
                o.MaxRetryCount = options.MaxRetryCount;
                o.LeaseSeconds = options.LeaseSeconds;
            });

            services.TryAddSingleton<IOutboxStore, MemoryOutboxStore>();
            services.TryAddSingleton<IInboxStore, MemoryInboxStore>();
            services.AddHostedService<OutboxBackgroundService>();
            return services;
        }
    }
}
