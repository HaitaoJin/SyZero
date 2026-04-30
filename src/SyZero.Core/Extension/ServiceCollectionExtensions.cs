using Dynamitey;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SyZero
{
    public static class ServiceCollectionExtensions
    {
        public static bool IsAdded<T>(this IServiceCollection services)
        {
            return services.IsAdded(typeof(T));
        }

        public static bool IsAdded(this IServiceCollection services, Type type)
        {
            return services.Any(d => d.ServiceType == type);
        }

        public static T GetSingletonInstanceOrNull<T>(this IServiceCollection services)
        {
            return (T)services
                .FirstOrDefault(d => d.ServiceType == typeof(T))
                ?.ImplementationInstance;
        }

        public static T GetSingletonInstance<T>(this IServiceCollection services)
        {
            var service = services.GetSingletonInstanceOrNull<T>();
            if (service == null)
            {
                throw new InvalidOperationException("Could not find singleton service: " + typeof(T).AssemblyQualifiedName);
            }

            return service;
        }

        public static IServiceProvider BuildServiceProviderFromFactory(this IServiceCollection services)
        {
            foreach (var service in services)
            {
                var factoryInterface = service.ImplementationInstance?.GetType()
                    .GetTypeInfo()
                    .GetInterfaces()
                    .FirstOrDefault(i => i.GetTypeInfo().IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(IServiceProviderFactory<>));

                if (factoryInterface == null)
                {
                    continue;
                }

                var containerBuilderType = factoryInterface.GenericTypeArguments[0];
                return (IServiceProvider)typeof(ServiceCollectionExtensions)
                    .GetTypeInfo()
                    .GetMethods()
                    .Single(m => m.Name == nameof(BuildServiceProviderFromFactory) && m.IsGenericMethod)
                    .MakeGenericMethod(containerBuilderType)
                    .Invoke(null, new object[] { services, null });
            }

            return services.BuildServiceProvider();
        }

        public static IServiceProvider BuildServiceProviderFromFactory<TContainerBuilder>(this IServiceCollection services, Action<TContainerBuilder> builderAction = null)
        {

            var serviceProviderFactory = services.GetSingletonInstanceOrNull<IServiceProviderFactory<TContainerBuilder>>();
            if (serviceProviderFactory == null)
            {
                throw new Exception($"Could not find {typeof(IServiceProviderFactory<TContainerBuilder>).FullName} in {services}.");
            }

            var builder = serviceProviderFactory.CreateBuilder(services);
            builderAction?.Invoke(builder);
            return serviceProviderFactory.CreateServiceProvider(builder);
        }

        public static List<TypeInfo> GetTypesAssignableTo(this Assembly assembly, Type compareType)
        {
            var typeInfoList = new List<TypeInfo>();
            try
            {
                IEnumerable<TypeInfo> definedTypes;
                try
                {
                    definedTypes = assembly.DefinedTypes;
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 当某些类型无法加载时，使用能够加载的类型
                    definedTypes = ex.Types.Where(t => t != null).Select(t => t.GetTypeInfo());
                }

                typeInfoList = definedTypes.Where(x => x != null
                                    && x.IsClass
                                    && !x.IsAbstract
                                    && x != compareType
                                    && SafeGetInterfaces(x)
                                            .Any(i => i.IsGenericType
                                                    && i.GetGenericTypeDefinition() == compareType))?.ToList() ?? new List<TypeInfo>();
            }
            catch
            {
                // 忽略无法加载的程序集
            }

            return typeInfoList;
        }

        private static Type[] SafeGetInterfaces(TypeInfo typeInfo)
        {
            try
            {
                return typeInfo.GetInterfaces();
            }
            catch
            {
                return Array.Empty<Type>();
            }
        }

        public static IServiceCollection AddClassesAsImplementedInterface(
                this IServiceCollection services,
                IEnumerable<Assembly> assemblys,
                Type compareType,
                ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            foreach (var assembly in assemblys)
            {
                assembly.GetTypesAssignableTo(compareType).ForEach((typeInfo) =>
                {
                    if (!typeInfo.IsGenericType)
                    {
                        var implementationType = typeInfo.AsType();
                        foreach (var implementedInterface in typeInfo.ImplementedInterfaces)
                        {
                            switch (lifetime)
                            {
                                case ServiceLifetime.Scoped:
                                    services.AddScoped(implementedInterface, implementationType);
                                    break;
                                case ServiceLifetime.Singleton:
                                    services.AddSingleton(implementedInterface, implementationType);
                                    break;
                                case ServiceLifetime.Transient:
                                    services.AddTransient(implementedInterface, implementationType);
                                    break;
                            }
                        }
                    }
                });
            }
            return services;
        }

        public static IServiceCollection AddClassesAsImplementedInterface(
        this IServiceCollection services,
        Assembly assembly,
        Type compareType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddClassesAsImplementedInterface(new List<Assembly>() { assembly }, compareType, lifetime);
        }

        public static IServiceCollection AddClassesAsImplementedInterface(
            this IServiceCollection services,
            Type compareType,
            ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            return services.AddClassesAsImplementedInterface(ReflectionHelper.GetAssemblies(), compareType, lifetime);
        }
    }
}