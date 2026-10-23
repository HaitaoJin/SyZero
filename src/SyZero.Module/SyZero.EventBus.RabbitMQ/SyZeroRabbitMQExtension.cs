using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SyZero.EventBus;
using SyZero.EventBus.RabbitMQ;
using System;

namespace SyZero
{
    /// <summary>
    /// SyZero RabbitMQ 扩展方法
    /// </summary>
    public static class SyZeroRabbitMQExtension
    {
        #region RabbitMQEventBus

        /// <summary>
        /// 添加 RabbitMQ 事件总线到依赖注入容器
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRabbitMQEventBus(
            this IServiceCollection services,
            RabbitMQEventBusOptions options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();
            services.AddSingleton(options);

            // 注册连接工厂
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                return new ConnectionFactory
                {
                    HostName = options.HostName,
                    Port = options.Port,
                    UserName = options.UserName,
                    Password = options.Password,
                    VirtualHost = options.VirtualHost,
                    ClientProvidedName = options.ClientProvidedName,
                    RequestedConnectionTimeout = TimeSpan.FromMilliseconds(options.RequestedConnectionTimeout),
                    RequestedHeartbeat = TimeSpan.FromSeconds(options.RequestedHeartbeat),
                    DispatchConsumersAsync = true,
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                };
            });

            // 注册持久化连接
            services.AddSingleton<RabbitMQPersistentConnection>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RabbitMQPersistentConnection>>();
                var factory = sp.GetRequiredService<IConnectionFactory>();

                var connection = new RabbitMQPersistentConnection(factory, logger, options.RetryCount);
                connection.TryConnect();
                return connection;
            });

            // 注册事件总线
            services.AddSingleton<IEventBus, RabbitMQEventBus>(sp =>
            {
                var persistentConnection = sp.GetRequiredService<RabbitMQPersistentConnection>();
                var logger = sp.GetRequiredService<ILogger<RabbitMQEventBus>>();
                var serviceProvider = sp;

                return new RabbitMQEventBus(persistentConnection, logger, serviceProvider, options);
            });

            return services;
        }

        /// <summary>
        /// 添加 RabbitMQ 事件总线到依赖注入容器（从配置读取）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置节名称，默认为 "RabbitMQ"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRabbitMQEventBus(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = RabbitMQEventBusOptions.SectionName)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            ArgumentNullException.ThrowIfNull(configuration);
            var options = new RabbitMQEventBusOptions();
            configuration.GetSection(sectionName).Bind(options);
            return AddRabbitMQEventBus(services, options);
        }

        /// <summary>
        /// 添加 RabbitMQ 事件总线到依赖注入容器（从配置读取，并支持额外配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托（在配置文件配置之后执行）</param>
        /// <param name="configuration">配置</param>
        /// <param name="sectionName">配置节名称，默认为 "RabbitMQ"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddRabbitMQEventBus(
            this IServiceCollection services,
            IConfiguration configuration,
            Action<RabbitMQEventBusOptions> optionsAction,
            string sectionName = RabbitMQEventBusOptions.SectionName)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            ArgumentNullException.ThrowIfNull(configuration);
            var options = new RabbitMQEventBusOptions();
            configuration.GetSection(sectionName).Bind(options);
            optionsAction?.Invoke(options);
            return AddRabbitMQEventBus(services, options);
        }

        #endregion
    }
}

