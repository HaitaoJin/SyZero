using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using SyZero.EventBus;
using SyZero.EventBus.LocalEventBus;

namespace SyZero
{
    /// <summary>
    /// 事件总线扩展方法
    /// </summary>
    public static class LocalEventBusExtension
    {
        /// <summary>
        /// 添加本地事件总线到依赖注入容器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLocalEventBus(this IServiceCollection services, LocalEventBusOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var instance = new LocalEventBus(options);
            services.AddSingleton<IEventBus>(instance);
            services.AddSingleton(instance);

            return services;
        }

        /// <summary>
        /// 添加本地事件总线到依赖注入容器（从配置读取）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称，默认为 "LocalEventBus"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLocalEventBus(this IServiceCollection services, IConfiguration configuration = null, string sectionName = LocalEventBusOptions.SectionName)
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new LocalEventBusOptions();
            config?.GetSection(sectionName)?.Bind(options);
            return services.AddLocalEventBus(options);
        }

        /// <summary>
        /// 添加本地事件总线到依赖注入容器（从配置读取，并支持额外配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托（在配置文件配置之后执行）</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称，默认为 "LocalEventBus"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddLocalEventBus(this IServiceCollection services, Action<LocalEventBusOptions> optionsAction, IConfiguration configuration = null, string sectionName = LocalEventBusOptions.SectionName)
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new LocalEventBusOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return services.AddLocalEventBus(options);
        }
    }
}
