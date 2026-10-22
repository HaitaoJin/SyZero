using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SyZero.Application.DynamicApi
{
    /// <summary>
    /// 动态 API Provider 共用的 <see cref="IServiceCollection"/> 扩展。
    /// </summary>
    public static class DynamicApiServiceCollectionExtensions
    {
        /// <summary>
        /// 检查指定服务类型是否已注册。
        /// </summary>
        public static bool IsAdded<T>(this IServiceCollection services)
        {
            return services.IsAdded(typeof(T));
        }

        /// <summary>
        /// 检查指定服务类型是否已注册。
        /// </summary>
        public static bool IsAdded(this IServiceCollection services, Type type)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return services.Any(d => d.ServiceType == type);
        }

        /// <summary>
        /// 获取已注册的单例实例（未注册返回 null）。
        /// </summary>
        public static T? GetSingletonInstanceOrNull<T>(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return (T?)services
                .FirstOrDefault(d => d.ServiceType == typeof(T))
                ?.ImplementationInstance;
        }

        /// <summary>
        /// 获取已注册的单例实例（未注册时抛出 <see cref="InvalidOperationException"/>）。
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

        /// <summary>
        /// 通过已注册的 <see cref="IServiceProviderFactory{TContainerBuilder}"/> 构建 <see cref="IServiceProvider"/>。
        /// </summary>
        public static IServiceProvider BuildServiceProviderFromFactory(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            foreach (var service in services)
            {
                var factoryInterface = service.ImplementationInstance?.GetType()
                    .GetTypeInfo()
                    .GetInterfaces()
                    .FirstOrDefault(i => i.GetTypeInfo().IsGenericType
                                         && i.GetGenericTypeDefinition() == typeof(IServiceProviderFactory<>));

                if (factoryInterface == null)
                {
                    continue;
                }

                var containerBuilderType = factoryInterface.GenericTypeArguments[0];
                return (IServiceProvider)typeof(DynamicApiServiceCollectionExtensions)
                    .GetTypeInfo()
                    .GetMethods()
                    .Single(m => m.Name == nameof(BuildServiceProviderFromFactory) && m.IsGenericMethod)
                    .MakeGenericMethod(containerBuilderType)
                    .Invoke(null, new object?[] { services, null })!;
            }

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// 使用指定 <see cref="IServiceProviderFactory{TContainerBuilder}"/> 构建容器。
        /// </summary>
        public static IServiceProvider BuildServiceProviderFromFactory<TContainerBuilder>(this IServiceCollection services, Action<TContainerBuilder>? builderAction = null) where TContainerBuilder : notnull
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var serviceProviderFactory = services.GetSingletonInstanceOrNull<IServiceProviderFactory<TContainerBuilder>>()
                ?? throw new InvalidOperationException($"找不到 {typeof(IServiceProviderFactory<TContainerBuilder>).FullName}。");

            var builder = serviceProviderFactory.CreateBuilder(services);
            builderAction?.Invoke(builder);
            return serviceProviderFactory.CreateServiceProvider(builder);
        }
    }
}
