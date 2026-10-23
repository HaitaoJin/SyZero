using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nacos.AspNetCore.V2;
using System;
using SyZero.ServiceDiscovery.Nacos;
using SyZero.Service;

namespace SyZero
{
    /// <summary>
    /// Nacos配置扩展
    /// </summary>
    public static class SyZeroNacosExtension
    {
        /// <summary>
        /// 从配置注册 Nacos，并注入 SyZero 的服务管理实现。
        /// </summary>
        public static IServiceCollection AddNacos(this IServiceCollection services, IConfiguration configuration, string section = "Nacos")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddSingleton<IServiceManagement, ServiceManagement>();
            return services.AddNacosAspNet(configuration, section);
        }

        /// <summary>
        /// 先从配置绑定，再应用额外委托配置。
        /// </summary>
        public static IServiceCollection AddNacos(this IServiceCollection services, IConfiguration configuration, Action<NacosAspNetOptions> optionsAction, string section = "Nacos")
        {
            if (optionsAction == null)
            {
                throw new ArgumentNullException(nameof(optionsAction));
            }

            ArgumentNullException.ThrowIfNull(configuration);

            services.AddSingleton<IServiceManagement, ServiceManagement>();

            return services.AddNacosAspNet(options =>
            {
                configuration.GetSection(section).Bind(options);
                optionsAction(options);
            });
        }
    }
}

