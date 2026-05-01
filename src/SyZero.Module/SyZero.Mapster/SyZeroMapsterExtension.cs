using System;
using System.Linq;
using System.Reflection;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SyZero
{
    /// <summary>
    /// SyZero Mapster 扩展方法。
    /// 提供 IObjectMapper 的 Mapster 实现，并自动扫描程序集中实现 IRegister 的映射规则。
    /// </summary>
    public static class SyZeroMapsterExtension
    {
        /// <summary>
        /// 添加 SyZero Mapster 服务（自动扫描所有程序集）。
        /// </summary>
        public static IServiceCollection AddSyZeroMapster(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            return AddSyZeroMapster(services, configAction: null, Array.Empty<Assembly>());
        }

        /// <summary>
        /// 添加 SyZero Mapster 服务（指定程序集）。
        /// </summary>
        public static IServiceCollection AddSyZeroMapster(this IServiceCollection services, params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(services);
            return AddSyZeroMapster(services, configAction: null, assemblies);
        }

        /// <summary>
        /// 添加 SyZero Mapster 服务（自定义配置 + 程序集）。
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configAction">在 IRegister 扫描完成后对 <see cref="TypeAdapterConfig"/> 做的额外配置</param>
        /// <param name="assemblies">要扫描的程序集；为空则使用 <c>ReflectionHelper.GetAssemblies()</c></param>
        public static IServiceCollection AddSyZeroMapster(
            this IServiceCollection services,
            Action<TypeAdapterConfig>? configAction,
            params Assembly[] assemblies)
        {
            ArgumentNullException.ThrowIfNull(services);

            var resolvedAssemblies = ResolveAssemblies(assemblies);

            var config = new TypeAdapterConfig();
            // 扫描所有程序集中的 IRegister 实现，自动注册映射规则
            config.Scan(resolvedAssemblies);
            configAction?.Invoke(config);

            services.TryAddSingleton(config);
            services.TryAddSingleton<IMapper>(sp => new ServiceMapper(sp, sp.GetRequiredService<TypeAdapterConfig>()));

            // 替换 SyZero 默认的 IObjectMapper
            services.Replace(ServiceDescriptor.Singleton<SyZero.ObjectMapper.IObjectMapper, SyZero.Mapster.MapsterObjectMapper>());

            return services;
        }

        private static Assembly[] ResolveAssemblies(Assembly[]? assemblies)
        {
            var resolvedAssemblies = assemblies?
                .OfType<Assembly>()
                .Distinct()
                .ToArray();

            return resolvedAssemblies is { Length: > 0 }
                ? resolvedAssemblies
                : ReflectionHelper.GetAssemblies().Distinct().ToArray();
        }
    }
}
