using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using SyZero.Client.RemoteService;
using SyZero.Feign;

namespace SyZero
{
    /// <summary>
    /// Feign 服务扩展方法
    /// </summary>
    public static class SyZeroFeignExtension
    {
        /// <summary>
        /// 默认 Feign 配置节名称（保留与历史一致）
        /// </summary>
        public const string FeignSectionName = "Feign";

        /// <summary>
        /// 添加 Feign 到依赖注入容器
        /// </summary>
        public static IServiceCollection AddSyZeroFeign(this IServiceCollection services, RemoteServiceOptions options)
        {
            return services.AddRemoteService(new FeignRemoteServiceProvider(), options);
        }

        /// <summary>
        /// 添加 Feign 到依赖注入容器（从配置读取）
        /// </summary>
        public static IServiceCollection AddSyZeroFeign(this IServiceCollection services, IConfiguration configuration, string sectionName = FeignSectionName)
        {
            return services.AddRemoteService(new FeignRemoteServiceProvider(), configuration, sectionName);
        }

        /// <summary>
        /// 添加 Feign 到依赖注入容器（从配置读取，并支持额外配置）
        /// </summary>
        public static IServiceCollection AddSyZeroFeign(this IServiceCollection services, Action<RemoteServiceOptions> optionsAction, IConfiguration configuration, string sectionName = FeignSectionName)
        {
            return services.AddRemoteService(new FeignRemoteServiceProvider(), optionsAction, configuration, sectionName);
        }
    }
}
