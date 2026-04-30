using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace SyZero.DynamicGrpc.Helpers
{
    /// <summary>
    /// 服务集合扩展方法
    /// </summary>
    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 检查服务是否已添加
        /// </summary>
        public static bool IsAdded<T>(this IServiceCollection services)
        {
            return services.IsAdded(typeof(T));
        }

        /// <summary>
        /// 检查服务是否已添加
        /// </summary>
        public static bool IsAdded(this IServiceCollection services, Type type)
        {
            return services.Any(d => d.ServiceType == type);
        }

        /// <summary>
        /// 获取单例实例或 null
        /// </summary>
        public static T GetSingletonInstanceOrNull<T>(this IServiceCollection services)
        {
            return (T)services
                .FirstOrDefault(d => d.ServiceType == typeof(T))
                ?.ImplementationInstance;
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static T GetSingletonInstance<T>(this IServiceCollection services)
        {
            var service = services.GetSingletonInstanceOrNull<T>();
            if (service == null)
            {
                throw new InvalidOperationException("找不到单例服务: " + typeof(T).AssemblyQualifiedName);
            }

            return service;
        }
    }
}
