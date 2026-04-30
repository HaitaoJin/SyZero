using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using SyZero.EventBus;
using SyZero.EventBus.DBEventBus;
using SyZero.EventBus.DBEventBus.Repository;
using SyZero.EventBus.LocalEventBus;

namespace SyZero
{
    /// <summary>
    /// 事件总线扩展方法
    /// </summary>
    public static class DBEventBusExtension
    {
        /// <summary>
        /// 添加数据库事件总线到依赖注入容器（使用默认 Repository 实现）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBEventBus(this IServiceCollection services, DBEventBusOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.AddSingleton(options);

            // 注册默认 Repository 实现
            services.AddSingleton<IEventSubscriptionRepository, EventSubscriptionRepository>();
            services.AddSingleton<IEventQueueRepository, EventQueueRepository>();
            services.AddSingleton<IDeadLetterEventRepository, DeadLetterEventRepository>();

            // 如果启用 Leader 选举，注册 Leader 选举 Repository
            if (options.EnableLeaderElection)
            {
                services.AddSingleton<ILeaderElectionRepository, LeaderElectionRepository>();
            }

            services.AddSingleton<IEventBus, DBEventBus>();
            services.AddSingleton<DBEventBus>();

            return services;
        }

        /// <summary>
        /// 添加数据库事件总线到依赖注入容器（使用默认 Repository 实现，从配置读取）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称，默认为 "DBEventBus"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBEventBus(this IServiceCollection services, IConfiguration configuration = null, string sectionName = DBEventBusOptions.SectionName)
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBEventBusOptions();
            config?.GetSection(sectionName)?.Bind(options);
            return services.AddDBEventBus(options);
        }

        /// <summary>
        /// 添加数据库事件总线到依赖注入容器（使用默认 Repository 实现，从配置读取，并支持额外配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托（在配置文件配置之后执行）</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称，默认为 "DBEventBus"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBEventBus(this IServiceCollection services, Action<DBEventBusOptions> optionsAction, IConfiguration configuration = null, string sectionName = DBEventBusOptions.SectionName)
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBEventBusOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return services.AddDBEventBus(options);
        }

        /// <summary>
        /// 添加数据库事件总线到依赖注入容器（使用自定义仓储实现）
        /// </summary>
        /// <typeparam name="TSubscriptionRepository">订阅仓储实现类型</typeparam>
        /// <typeparam name="TEventQueueRepository">事件队列仓储实现类型</typeparam>
        /// <typeparam name="TDeadLetterRepository">死信队列仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBEventBus<TSubscriptionRepository, TEventQueueRepository, TDeadLetterRepository>(
            this IServiceCollection services,
            DBEventBusOptions options)
            where TSubscriptionRepository : class, IEventSubscriptionRepository
            where TEventQueueRepository : class, IEventQueueRepository
            where TDeadLetterRepository : class, IDeadLetterEventRepository
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.AddSingleton(options);
            services.AddSingleton<IEventSubscriptionRepository, TSubscriptionRepository>();
            services.AddSingleton<IEventQueueRepository, TEventQueueRepository>();
            services.AddSingleton<IDeadLetterEventRepository, TDeadLetterRepository>();
            services.AddSingleton<IEventBus, DBEventBus>();
            services.AddSingleton<DBEventBus>();

            return services;
        }

        /// <summary>
        /// 添加数据库事件总线到依赖注入容器（使用自定义仓储实现，从配置读取）
        /// </summary>
        /// <typeparam name="TSubscriptionRepository">订阅仓储实现类型</typeparam>
        /// <typeparam name="TEventQueueRepository">事件队列仓储实现类型</typeparam>
        /// <typeparam name="TDeadLetterRepository">死信队列仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBEventBus<TSubscriptionRepository, TEventQueueRepository, TDeadLetterRepository>(
            this IServiceCollection services,
            Action<DBEventBusOptions> optionsAction = null,
            IConfiguration configuration = null,
            string sectionName = DBEventBusOptions.SectionName)
            where TSubscriptionRepository : class, IEventSubscriptionRepository
            where TEventQueueRepository : class, IEventQueueRepository
            where TDeadLetterRepository : class, IDeadLetterEventRepository
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBEventBusOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return services.AddDBEventBus<TSubscriptionRepository, TEventQueueRepository, TDeadLetterRepository>(options);
        }

        /// <summary>
        /// 添加数据库事件总线到依赖注入容器（使用自定义仓储实现，包括 Leader 选举仓储）
        /// </summary>
        /// <typeparam name="TSubscriptionRepository">订阅仓储实现类型</typeparam>
        /// <typeparam name="TEventQueueRepository">事件队列仓储实现类型</typeparam>
        /// <typeparam name="TDeadLetterRepository">死信队列仓储实现类型</typeparam>
        /// <typeparam name="TLeaderElectionRepository">Leader 选举仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBEventBus<TSubscriptionRepository, TEventQueueRepository, TDeadLetterRepository, TLeaderElectionRepository>(
            this IServiceCollection services,
            DBEventBusOptions options)
            where TSubscriptionRepository : class, IEventSubscriptionRepository
            where TEventQueueRepository : class, IEventQueueRepository
            where TDeadLetterRepository : class, IDeadLetterEventRepository
            where TLeaderElectionRepository : class, ILeaderElectionRepository
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.AddSingleton(options);
            services.AddSingleton<IEventSubscriptionRepository, TSubscriptionRepository>();
            services.AddSingleton<IEventQueueRepository, TEventQueueRepository>();
            services.AddSingleton<IDeadLetterEventRepository, TDeadLetterRepository>();
            services.AddSingleton<ILeaderElectionRepository, TLeaderElectionRepository>();
            services.AddSingleton<IEventBus, DBEventBus>();
            services.AddSingleton<DBEventBus>();

            return services;
        }

        /// <summary>
        /// 添加数据库事件总线到依赖注入容器（使用自定义仓储实现，包括 Leader 选举仓储，从配置读取）
        /// </summary>
        /// <typeparam name="TSubscriptionRepository">订阅仓储实现类型</typeparam>
        /// <typeparam name="TEventQueueRepository">事件队列仓储实现类型</typeparam>
        /// <typeparam name="TDeadLetterRepository">死信队列仓储实现类型</typeparam>
        /// <typeparam name="TLeaderElectionRepository">Leader 选举仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBEventBus<TSubscriptionRepository, TEventQueueRepository, TDeadLetterRepository, TLeaderElectionRepository>(
            this IServiceCollection services,
            Action<DBEventBusOptions> optionsAction = null,
            IConfiguration configuration = null,
            string sectionName = DBEventBusOptions.SectionName)
            where TSubscriptionRepository : class, IEventSubscriptionRepository
            where TEventQueueRepository : class, IEventQueueRepository
            where TDeadLetterRepository : class, IDeadLetterEventRepository
            where TLeaderElectionRepository : class, ILeaderElectionRepository
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBEventBusOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return services.AddDBEventBus<TSubscriptionRepository, TEventQueueRepository, TDeadLetterRepository, TLeaderElectionRepository>(options);
        }
    }
}
