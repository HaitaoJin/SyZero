using System;
using System.Threading.Tasks;
using CacheManager.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using Ocelot.ServiceDiscovery;
using SyZero.Gateway;
using SyZero.Gateway.Ocelot;
using SyZero.Service;
using LoggerExtensions = Microsoft.Extensions.Logging;

namespace SyZero
{
    /// <summary>
    /// SyZero.Gateway.Ocelot 模块的注册入口。
    /// </summary>
    public static class SyZeroOcelotExtension
    {
        public static IServiceCollection AddSyZeroOcelot(this IServiceCollection services, IConfiguration? configuration = null)
        {
            return AddSyZeroOcelot(services, _ => { }, configuration);
        }

        public static IServiceCollection AddSyZeroOcelot(
            this IServiceCollection services,
            Action<SyZeroOcelotOptions> optionsAction,
            IConfiguration? configuration = null)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            var config = configuration
                ?? AppConfig.Configuration
                ?? throw new InvalidOperationException("Configuration is required before registering SyZero.Gateway.Ocelot.");

            var options = SyZeroOcelotOptions.CreateDefault(config);
            optionsAction?.Invoke(options);
            options.Validate();

            services.AddSingleton(options);
            services.AddSingleton<GatewayOptions>(options);
            services.AddHttpContextAccessor();
            services.AddCors(corsOptions =>
            {
                corsOptions.AddPolicy(options.CorsPolicyName, policy => ConfigureCors(policy, options));
            });

            var ocelotBuilder = services.AddOcelot(config);
            if (options.EnableConsul)
            {
                ocelotBuilder = options.UseConsulServiceAddress
                    ? ocelotBuilder.AddConsul<SyZeroConsulServiceBuilder>()
                    : ocelotBuilder.AddConsul();

                if (options.EnableConfigStoredInConsul)
                {
                    ocelotBuilder = ocelotBuilder.AddConfigStoredInConsul();
                }
            }

            if (options.EnableCacheManager)
            {
                ocelotBuilder = ocelotBuilder.AddCacheManager(settings => settings.WithDictionaryHandle());
            }

            if (options.EnablePolly)
            {
                ocelotBuilder = ocelotBuilder.AddPolly();
            }

            if (options.EnableSyZeroServiceDiscovery)
            {
                // Ocelot 23 的服务发现走 ServiceDiscoveryProviderFactory，工厂从 DI 取一个
                // ServiceDiscoveryFinderDelegate；我们把工厂返回 SyZeroDiscovery 即可。
                // ocelot.json 中需要把 GlobalConfiguration.ServiceDiscoveryProvider.Type
                // 设为 "SyZeroDiscovery"（大小写不敏感）。
                services.AddSingleton<ServiceDiscoveryFinderDelegate>((sp, _, route) =>
                    new SyZeroDiscovery(
                        sp.GetRequiredService<IServiceManagement>(),
                        route.ServiceName,
                        sp.GetService<LoggerExtensions.ILogger<SyZeroDiscovery>>()));
            }

            if (options.EnableSwagger)
            {
                services.AddSwaggerForOcelot(config);

                if (options.EnableSwaggerGen)
                {
                    services.AddSwaggerGen(swagger =>
                    {
                        swagger.SwaggerDoc(options.SwaggerDocumentName, new OpenApiInfo
                        {
                            Title = options.SwaggerTitle,
                            Version = options.SwaggerDocumentName
                        });
                    });
                }
            }

            // 注册 SyZero 网关抽象的 Ocelot 实现
            services.TryAddSingleton<IGatewayConfigurationApplier, OcelotConfigurationApplier>();
            services.TryAddSingleton<IGatewayConfigurationStore, OcelotConfigurationStore>();
            services.TryAddSingleton<IGateway, OcelotGateway>();

            return services;
        }

        public static async Task<IApplicationBuilder> UseSyZeroOcelotAsync(this IApplicationBuilder app)
        {
            if (app is null) throw new ArgumentNullException(nameof(app));

            var options = app.ApplicationServices.GetRequiredService<SyZeroOcelotOptions>();
            if (options.EnableSwagger)
            {
                app.UseSwaggerForOcelotUI(swagger =>
                {
                    swagger.PathToSwaggerGenerator = options.SwaggerGeneratorPath;
                });
            }

            app.UseWebSockets();
            await app.UseOcelot();

            return app;
        }

        private static void ConfigureCors(CorsPolicyBuilder policy, SyZeroOcelotOptions options)
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
