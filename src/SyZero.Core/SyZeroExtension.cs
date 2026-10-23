using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Reflection;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using SyZero.Dependency;
using SyZero.Domain.Repository;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;
using SyZero.Serialization;
using SyZero.Service;

namespace SyZero
{
    public static class SyZeroExtension
    {
        public static IHost UseSyZero(this IHost app)
        {
            return app;
        }

        public static IHostApplicationBuilder AddSyZero(this IHostApplicationBuilder builder)
        {
            builder.Services.AddSyZero(builder.Configuration);
            return builder;
        }

        /// <summary>
        /// SyZeroModule
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddSyZero(this IServiceCollection services, IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            System.Console.WriteLine(@" _____  __    __  ______  _____   _____    _____  
/  ___/ \ \  / / |___  / | ____| |  _  \  /  _  \ 
| |___   \ \/ /     / /  | |__   | |_| |  | | | | 
\___  \   \  /     / /   |  __|  |  _  /  | | | | 
 ___| |   / /     / /__  | |___  | | \ \  | |_| | 
/_____/  /_/     /_____| |_____| |_|  \_\ \_____/ ");
            // 获取当前程序集
            System.Console.WriteLine("");
            System.Console.WriteLine("版本号: " + Assembly.GetExecutingAssembly().GetName().Version);
            System.Console.WriteLine("启动中......");

            // 核心包可被网关等无认证、无持久化宿主独立使用；功能模块注册后会覆盖这些占位实现。
            services.TryAddSingleton<IToken, NullToken>();
            services.TryAddScoped<IUnitOfWork, UnsupportedUnitOfWork>();

            //注入SySession
            services.AddScoped<ISySession, SySession>();
            services.AddSyZeroValidation();
            services.AddOptions();
            services.AddOptions<Configurations.SyZeroServerOptions>().Bind(configuration.GetSection("SyZero"));
            services.PostConfigure<Configurations.SyZeroServerOptions>(options =>
            {
                var localIp = ResolveLocalIPv4() ?? "127.0.0.1";
                if (string.IsNullOrWhiteSpace(options.Ip)) options.Ip = localIp;
                if (string.IsNullOrWhiteSpace(options.WanIp)) options.WanIp = localIp;
            });
            services.AddOptions<Configurations.SyZeroConnectionOptions>().Bind(configuration.GetSection("ConnectionString"));
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            //// 获取所有实现了IScopedDependency接口的类型
            //var scopedDependency = ReflectionHelper.GetTypes()
            //                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IScopedDependency).IsAssignableFrom(t));
            //foreach (var type in scopedDependency)
            //{
            //    services.AddScoped(type.GetInterfaces().Last(), type);
            //}

            //// 获取所有实现了ISingletonDependency接口的类型
            //var singletonDependencyTypes = ReflectionHelper.GetTypes()
            //                    .Where(t => t.IsClass && !t.IsAbstract && typeof(ISingletonDependency).IsAssignableFrom(t));
            //foreach (var type in singletonDependencyTypes)
            //{
            //    services.AddSingleton(type.GetInterfaces().Last(), type);
            //}

            //// 获取所有实现了ITransientDependency接口的类型
            //var transientDependency = ReflectionHelper.GetTypes()
            //                    .Where(t => t.IsClass && !t.IsAbstract && typeof(ITransientDependency).IsAssignableFrom(t));
            //foreach (var type in transientDependency)
            //{
            //    services.AddScoped(type.GetInterfaces().Last(), type);
            //}
            services.AddClassesAsImplementedInterface(typeof(IScopedDependency), ServiceLifetime.Scoped);
            services.AddClassesAsImplementedInterface(typeof(ISingletonDependency), ServiceLifetime.Singleton);
            services.AddClassesAsImplementedInterface(typeof(ITransientDependency), ServiceLifetime.Transient);

            // 注册框架启动钩子（异步注册 / 注销服务到服务管理中心）
            services.AddHostedService<SyZeroBootstrapper>();

            // 获取所有继承SyZeroServiceBase类的类型
            var syZeroServiceBaseTypes = ReflectionHelper.GetTypes()
                                .Where(t => t.IsClass && !t.IsAbstract && typeof(SyZeroServiceBase).IsAssignableFrom(t));
            foreach (var type in syZeroServiceBaseTypes)
            {
                services.AddScoped(type);
                
                var interfaceType = type.GetInterfaces().FirstOrDefault(i => i.Name == "I" + type.Name);
                if (interfaceType != null)
                {
                    services.AddScoped(interfaceType, provider => provider.GetService(type));
                }
            }

            return services;
        }

        private static string? ResolveLocalIPv4()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Select(network => network.GetIPProperties())
                .SelectMany(properties => properties.UnicastAddresses)
                .Where(address => address.Address.AddressFamily == AddressFamily.InterNetwork
                    && (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        || address.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Preferred)
                    && !System.Net.IPAddress.IsLoopback(address.Address))
                .Select(address => address.Address.ToString())
                .FirstOrDefault();
        }
    }
}
