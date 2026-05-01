using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SyZero.AspNetCore.Controllers;
using SyZero.AspNetCore.Middleware;

namespace SyZero
{
    public static class SyZeroAspNetExtension
    {
        /// <summary>
        /// 注册SyZeroControllerModule
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddSyZeroController(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddScoped<SyAuthMiddleware>();

            // 获取所有实现了SyZeroController的类型
            foreach (var type in GetControllerTypes())
            {
                if (!services.IsAdded(type))
                {
                    services.AddScoped(type);
                }
            }

            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            return services;
        }

        /// <summary>
        /// 注册 <see cref="SyExceptionMiddleware"/>（全局异常 → ProblemDetails）。
        /// </summary>
        public static IServiceCollection AddSyZeroExceptionHandler(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.TryAddSingleton<SyExceptionMiddleware>();
            return services;
        }

        /// <summary>
        /// 启用 <see cref="SyExceptionMiddleware"/>，建议放在管线最前。
        /// </summary>
        public static IApplicationBuilder UseSyZeroExceptionHandler(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);
            return app.UseMiddleware<SyExceptionMiddleware>();
        }

        private static IEnumerable<Type> GetControllerTypes()
        {
            foreach (var assembly in ReflectionHelper.GetAssemblies())
            {
                IEnumerable<Type> types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(type => type != null);
                }
                catch
                {
                    continue;
                }

                foreach (var type in types.Where(type =>
                             type != null
                             && type.IsClass
                             && !type.IsAbstract
                             && !type.ContainsGenericParameters
                             && typeof(SyZeroController).IsAssignableFrom(type)))
                {
                    yield return type;
                }
            }
        }
    }
}
