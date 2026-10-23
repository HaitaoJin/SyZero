using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Idempotency;

namespace SyZero
{
    /// <summary>
    /// SyZero.Idempotency 注册扩展。
    /// </summary>
    public static class SyZeroIdempotencyExtension
    {
        /// <summary>
        /// 注册幂等中间件 + 内存默认存储；引入 SyZero.Redis 时可用 Redis 存储覆盖。
        /// </summary>
        public static IServiceCollection AddSyZeroIdempotency(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<IdempotencyOptions>? configure = null,
            string sectionName = IdempotencyOptions.SectionName)
        {
            ArgumentNullException.ThrowIfNull(services);

            var options = new IdempotencyOptions();
            configuration?.GetSection(sectionName)?.Bind(options);
            configure?.Invoke(options);

            services.AddOptions<IdempotencyOptions>().Configure(o =>
            {
                o.HeaderName = options.HeaderName;
                o.ExpirationSeconds = options.ExpirationSeconds;
                o.InflightRejectionStatusCode = options.InflightRejectionStatusCode;
                o.ConflictStatusCode = options.ConflictStatusCode;
                o.MaxResponseBodyBytes = options.MaxResponseBodyBytes;
                o.MaxRequestBodyBytes = options.MaxRequestBodyBytes;
                o.ScopeKeyToRequest = options.ScopeKeyToRequest;
                o.Methods = options.Methods;
            }).Validate(o => !string.IsNullOrWhiteSpace(o.HeaderName), "HeaderName 不能为空")
              .Validate(o => o.ExpirationSeconds > 0, "ExpirationSeconds 必须大于 0")
              .Validate(o => o.MaxResponseBodyBytes > 0 && o.MaxRequestBodyBytes > 0, "请求/响应大小限制必须大于 0")
              .ValidateOnStart();

            services.TryAddSingleton<IIdempotencyStore, MemoryIdempotencyStore>();
            services.AddSingleton<IdempotencyMiddleware>();
            return services;
        }

        /// <summary>
        /// 启用幂等中间件（建议放在路由之后、终结点之前）。
        /// </summary>
        public static IApplicationBuilder UseSyZeroIdempotency(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            return app.UseMiddleware<IdempotencyMiddleware>();
        }
    }
}
