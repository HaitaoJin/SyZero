using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Reflection;
using SyZero.Dependency;
using SyZero.Runtime.Session;
using SyZero.Serialization;
using SyZero.Service;
using SyZero.Util;

namespace SyZero
{
    public static class SyZeroExtension
    {
        public static IHost UseSyZero(this IHost app)
        {
            // 设置全局服务提供者；
            // 服务注册 / 注销改由 SyZeroBootstrapper（IHostedService）以异步方式完成，
            // 不再在此处通过 .GetAwaiter().GetResult() 同步阻塞。
            SyZeroUtil.ServiceProvider = app.Services;
            return app;
        }

        public static IHostApplicationBuilder AddSyZero(this IHostApplicationBuilder builder)
        {
            AppConfig.Configuration = builder.Configuration;
            builder.Services.AddSyZero();
            return builder;
        }

        /// <summary>
        /// SyZeroModule
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddSyZero(this IServiceCollection services)
        {
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

            //注入SySession
            services.AddScoped<ISySession, SySession>();

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
    }
}
