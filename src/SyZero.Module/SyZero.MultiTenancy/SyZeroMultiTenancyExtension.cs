using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyZero.MultiTenancy.Resolvers;

namespace SyZero.MultiTenancy
{
    /// <summary>SyZero.MultiTenancy 注册与启用扩展。</summary>
    public static class SyZeroMultiTenancyExtension
    {
        /// <summary>
        /// 注册多租户基础设施（CurrentTenant / 内置 3 种 Resolver / DefaultTenantConnectionResolver）。
        /// 业务侧需自行注册一个 <see cref="ITenantStore"/>（如 <see cref="InMemoryTenantStore"/> 或自实现）。
        /// </summary>
        public static IServiceCollection AddSyZeroMultiTenancy(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            Action<MultiTenancyOptions>? configure = null)
        {
            var section = configuration?.GetSection("MultiTenancy");
            if (section != null) services.Configure<MultiTenancyOptions>(section);
            if (configure != null) services.Configure(configure);

            services.AddHttpContextAccessor();

            // 三个内置 Resolver 按「Header → Domain → Claim」顺序解析，命中即停。
            services.AddScoped<ITenantResolver, HeaderTenantResolver>();
            services.AddScoped<ITenantResolver, DomainTenantResolver>();
            services.AddScoped<ITenantResolver, ClaimTenantResolver>();

            return services;
        }

        /// <summary>
        /// 启用 <see cref="MultiTenancyMiddleware"/>。请放在 UseAuthentication 之后（Claim 解析需要已认证用户）、
        /// 在业务 Endpoint 之前。
        /// </summary>
        public static IApplicationBuilder UseSyZeroMultiTenancy(this IApplicationBuilder app)
            => app.UseMiddleware<MultiTenancyMiddleware>();
    }
}
