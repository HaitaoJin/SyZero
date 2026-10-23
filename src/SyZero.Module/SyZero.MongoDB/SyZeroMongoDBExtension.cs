using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SyZero.Domain.Repository;
using System;

namespace SyZero
{
    /// <summary>
    /// MongoDB 扩展方法
    /// </summary>
    public static class SyZeroMongoDBExtension
    {
        /// <summary>
        /// 使用 MongoDB 配置节注册服务
        /// </summary>
        public static IServiceCollection AddSyZeroMongoDB(this IServiceCollection services, IConfiguration configuration, string sectionName = "MongoDB")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var options = new SyZero.MongoDB.MongoOptions();
            configuration.GetSection(sectionName).Bind(options);
            return services.AddSyZeroMongoDB(options);
        }

        /// <summary>
        /// 使用默认配置并允许额外覆盖注册服务
        /// </summary>
        public static IServiceCollection AddSyZeroMongoDB(this IServiceCollection services, IConfiguration configuration, Action<SyZero.MongoDB.MongoOptions> optionsAction, string sectionName = "MongoDB")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var options = new SyZero.MongoDB.MongoOptions();
            configuration.GetSection(sectionName).Bind(options);
            optionsAction?.Invoke(options);
            return services.AddSyZeroMongoDB(options);
        }

        private static IServiceCollection AddSyZeroMongoDB(this IServiceCollection services, SyZero.MongoDB.MongoOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();

            services.AddSingleton(Options.Create(options));
            services.TryAddSingleton<SyZero.MongoDB.IMongoContext, SyZero.MongoDB.MongoContext>();
            services.TryAddScoped(typeof(IRepository<>), typeof(SyZero.MongoDB.MongoRepository<>));
            services.TryAddScoped(typeof(IRepository<,>), typeof(SyZero.MongoDB.MongoRepository<,>));

            return services;
        }
    }
}
