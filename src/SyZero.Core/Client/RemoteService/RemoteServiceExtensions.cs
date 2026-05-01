using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Client.RemoteService;

namespace SyZero
{
    /// <summary>
    /// 远程服务调用注册扩展
    /// </summary>
    public static class RemoteServiceExtensions
    {
        /// <summary>
        /// 使用指定的 Provider 注册远程服务
        /// </summary>
        public static IServiceCollection AddRemoteService(
            this IServiceCollection services,
            IRemoteServiceProvider provider,
            RemoteServiceOptions options)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.Validate();
            provider.Register(services, options);
            return services;
        }

        /// <summary>
        /// 使用指定的 Provider 注册远程服务（从配置读取）
        /// </summary>
        public static IServiceCollection AddRemoteService<TProvider>(
            this IServiceCollection services,
            IConfiguration? configuration = null,
            string sectionName = RemoteServiceOptions.SectionName)
            where TProvider : IRemoteServiceProvider, new()
        {
            return services.AddRemoteService(new TProvider(), configuration, sectionName);
        }

        /// <summary>
        /// 使用指定的 Provider 实例注册远程服务（从配置读取）
        /// </summary>
        public static IServiceCollection AddRemoteService(
            this IServiceCollection services,
            IRemoteServiceProvider provider,
            IConfiguration? configuration = null,
            string sectionName = RemoteServiceOptions.SectionName)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var config = configuration ?? AppConfig.Configuration
                ?? throw new InvalidOperationException("未找到远程服务配置源（IConfiguration 为空，且 AppConfig.Configuration 未初始化）。");

            var options = new RemoteServiceOptions();
            config.GetSection(sectionName).Bind(options);
            return services.AddRemoteService(provider, options);
        }

        /// <summary>
        /// 使用指定的 Provider 注册远程服务（从配置读取并支持额外配置）
        /// </summary>
        public static IServiceCollection AddRemoteService<TProvider>(
            this IServiceCollection services,
            Action<RemoteServiceOptions> optionsAction,
            IConfiguration? configuration = null,
            string sectionName = RemoteServiceOptions.SectionName)
            where TProvider : IRemoteServiceProvider, new()
        {
            return services.AddRemoteService(new TProvider(), optionsAction, configuration, sectionName);
        }

        /// <summary>
        /// 使用指定的 Provider 实例注册远程服务（从配置读取并支持额外配置）
        /// </summary>
        public static IServiceCollection AddRemoteService(
            this IServiceCollection services,
            IRemoteServiceProvider provider,
            Action<RemoteServiceOptions> optionsAction,
            IConfiguration? configuration = null,
            string sectionName = RemoteServiceOptions.SectionName)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var config = configuration ?? AppConfig.Configuration
                ?? throw new InvalidOperationException("未找到远程服务配置源（IConfiguration 为空，且 AppConfig.Configuration 未初始化）。");

            var options = new RemoteServiceOptions();
            config.GetSection(sectionName).Bind(options);
            optionsAction?.Invoke(options);
            return services.AddRemoteService(provider, options);
        }
    }
}
