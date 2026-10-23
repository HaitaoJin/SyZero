using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.RateLimiting;

namespace SyZero
{
    /// <summary>
    /// SyZero.RateLimiting 扩展方法。
    /// </summary>
    public static class SyZeroRateLimitingExtension
    {
        /// <summary>
        /// 注册 SyZero 限流：默认包括进程内 <see cref="IDistributedRateLimiter"/> 实现，
        /// 以及一个固定窗口的全局限流策略（可在 <see cref="SyZeroRateLimitingOptions"/> 中关闭）。
        /// </summary>
        public static IServiceCollection AddSyZeroRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<SyZeroRateLimitingOptions>? configure = null,
            string sectionName = SyZeroRateLimitingOptions.SectionName)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var options = new SyZeroRateLimitingOptions();
            configuration.GetSection(sectionName).Bind(options);
            configure?.Invoke(options);
            services.AddSingleton(options);

            services.TryAddSingleton<IDistributedRateLimiter, LocalDistributedRateLimiter>();

            services.AddRateLimiter(rl =>
            {
                rl.RejectionStatusCode = options.RejectionStatusCode;

                if (options.EnableGlobal)
                {
                    rl.AddPolicy(options.GlobalPolicyName, ctx =>
                    {
                        var partitionKey = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = options.GlobalPermitLimit,
                            Window = TimeSpan.FromSeconds(options.GlobalWindowSeconds),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true,
                        });
                    });
                }
            });

            return services;
        }

        /// <summary>
        /// 启用 SyZero 限流中间件。须在 <c>UseRouting</c> 之后、<c>UseEndpoints</c>/<c>MapXxx</c> 之前调用。
        /// </summary>
        public static IApplicationBuilder UseSyZeroRateLimiting(this IApplicationBuilder app)
        {
            return app.UseRateLimiter();
        }
    }
}
