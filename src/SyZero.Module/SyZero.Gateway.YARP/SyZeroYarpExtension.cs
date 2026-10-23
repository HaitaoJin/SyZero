using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SyZero.Gateway;
using SyZero.Gateway.YARP;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.ServiceDiscovery;

namespace SyZero
{
    /// <summary>
    /// SyZero.Gateway.YARP 模块的注册入口。
    /// </summary>
    public static class SyZeroYarpExtension
    {
        public static IServiceCollection AddSyZeroYarp(this IServiceCollection services, IConfiguration configuration)
        {
            return AddSyZeroYarp(services, _ => { }, configuration);
        }

        public static IServiceCollection AddSyZeroYarp(
            this IServiceCollection services,
            Action<SyZeroYarpOptions> optionsAction,
            IConfiguration configuration)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            ArgumentNullException.ThrowIfNull(configuration);
            var config = configuration;

            var options = SyZeroYarpOptions.CreateDefault(config);
            optionsAction?.Invoke(options);
            options.Validate();

            services.AddSingleton(options);
            services.AddSingleton<GatewayOptions>(options);
            services.AddHttpContextAccessor();
            services.AddCors(corsOptions =>
            {
                corsOptions.AddPolicy(options.CorsPolicyName, policy => ConfigureCors(policy, options));
            });

            // 单例的内存型配置提供者：运行时由 Applier.Update 热更新。
            var provider = new SyZeroYarpProxyConfigProvider();
            services.AddSingleton(provider);
            services.AddSingleton<IProxyConfigProvider>(provider);

            var proxyBuilder = services.AddReverseProxy();
            if (options.LoadFromReverseProxySection)
            {
                proxyBuilder.LoadFromConfig(config.GetSection("ReverseProxy"));
            }

            // 注册 SyZero 网关抽象的 YARP 实现
            services.TryAddSingleton<IGatewayConfigurationApplier, YarpConfigurationApplier>();
            services.TryAddSingleton<IGatewayConfigurationStore, YarpConfigurationStore>();
            services.TryAddSingleton<IGateway, YarpGateway>();

            return services;
        }

        /// <summary>
        /// 启用基于 <see cref="Service.IServiceManagement"/> 的 YARP 目标解析（自动桥接 Consul / Nacos /
        /// Redis / Local / DB 等 SyZero 服务发现后端）。需要先调用 <c>AddSyZeroYarp</c> 与对应的
        /// <c>AddConsul()</c> / <c>AddNacos()</c> / <c>AddRedisServiceManagement()</c> 等扩展。
        /// </summary>
        public static IServiceCollection AddSyZeroYarpServiceDiscovery(this IServiceCollection services)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            services.AddSingleton<IDestinationResolver, SyZeroYarpServiceDiscoveryDestinationResolver>();
            return services;
        }

        /// <summary>
        /// 在管道中映射 YARP 反向代理终端。注意必须放在 <c>UseRouting</c> 之后。
        /// 启动时会调用 <see cref="IGatewayConfigurationStore.ReloadAsync"/> 把
        /// <c>configuration.json</c> 装载到内存提供者；若文件不存在则保持空路由表。
        /// </summary>
        public static IEndpointRouteBuilder MapSyZeroYarp(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints is null) throw new ArgumentNullException(nameof(endpoints));

            var options = endpoints.ServiceProvider.GetRequiredService<SyZeroYarpOptions>();
            if (options.LoadFromConfigurationFile)
            {
                var store = endpoints.ServiceProvider.GetRequiredService<IGatewayConfigurationStore>();
                // 同步等待初始装载，保证首个请求能命中路由。
                store.ReloadAsync().GetAwaiter().GetResult();
            }

            endpoints.MapReverseProxy();
            return endpoints;
        }

        /// <summary>
        /// 启动时初始化 YARP 路由（不映射终端），常配合 <c>app.UseEndpoints</c> 内部 <see cref="MapSyZeroYarp"/> 使用。
        /// 若你直接调用了 <see cref="MapSyZeroYarp"/>，则无需再调用本方法。
        /// </summary>
        public static async Task<IApplicationBuilder> UseSyZeroYarpAsync(this IApplicationBuilder app)
        {
            if (app is null) throw new ArgumentNullException(nameof(app));

            var options = app.ApplicationServices.GetRequiredService<SyZeroYarpOptions>();
            if (options.LoadFromConfigurationFile)
            {
                var store = app.ApplicationServices.GetRequiredService<IGatewayConfigurationStore>();
                await store.ReloadAsync().ConfigureAwait(false);
            }
            return app;
        }

        private static void ConfigureCors(CorsPolicyBuilder policy, SyZeroYarpOptions options)
        {
            if (options.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(options.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                return;
            }

            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    }
}
