using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using SyZero.Service;
using SyZero.Service.LocalServiceManagement;

namespace SyZero
{
    /// <summary>
    /// 服务管理扩展方法
    /// </summary>
    public static class LocalServiceManagementExtension
    {
        #region LocalServiceManagement

        /// <summary>
        /// 添加本地服务管理到依赖注入容器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLocalServiceManagement(this IServiceCollection services, LocalServiceManagementOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();

            services.AddSingleton<LocalServiceManagement>(sp =>
                new LocalServiceManagement(options, sp.GetService<ILogger<LocalServiceManagement>>()));
            services.AddSingleton<IServiceManagement>(sp => sp.GetRequiredService<LocalServiceManagement>());

            return services;
        }

        /// <summary>
        /// 添加本地服务管理到依赖注入容器（从配置读取）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置节名称，默认为 "LocalServiceManagement"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLocalServiceManagement(this IServiceCollection services, IConfiguration configuration, string sectionName = LocalServiceManagementOptions.SectionName)
        {
            var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var options = new LocalServiceManagementOptions();
            config?.GetSection(sectionName)?.Bind(options);
            return AddLocalServiceManagement(services, options);
        }

        /// <summary>
        /// 添加本地服务管理到依赖注入容器（从配置读取，并支持额外配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托（在配置文件配置之后执行）</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置节名称，默认为 "LocalServiceManagement"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLocalServiceManagement(this IServiceCollection services, Action<LocalServiceManagementOptions> optionsAction, IConfiguration configuration, string sectionName = LocalServiceManagementOptions.SectionName)
        {
            var config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            var options = new LocalServiceManagementOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return AddLocalServiceManagement(services, options);
        }

        #endregion
    }
}
