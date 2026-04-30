using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using SyZero.Service;
using SyZero.Service.DBServiceManagement;
using SyZero.Service.DBServiceManagement.Repository;

namespace SyZero
{
    /// <summary>
    /// 服务管理扩展方法
    /// </summary>
    public static class DBServiceManagementExtension
    {
        #region DBServiceManagement

        /// <summary>
        /// 添加数据库服务管理到依赖注入容器（使用默认 Repository 实现）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBServiceManagement(this IServiceCollection services, DBServiceManagementOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();

            services.AddSingleton(options);
            
            // 注册默认 Repository 实现
            services.AddSingleton<IServiceRegistryRepository, ServiceRegistryRepository>();
            
            // 如果启用 Leader 选举，注册 Leader 选举 Repository
            if (options.EnableLeaderElection)
            {
                services.AddSingleton<ILeaderElectionRepository, LeaderElectionRepository>();
            }
            
            services.AddSingleton<IServiceManagement, DBServiceManagement>();
            services.AddSingleton<DBServiceManagement>();

            return services;
        }

        /// <summary>
        /// 添加数据库服务管理到依赖注入容器（使用默认 Repository 实现，从配置读取）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称，默认为 "DBServiceManagement"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBServiceManagement(this IServiceCollection services, IConfiguration configuration = null, string sectionName = DBServiceManagementOptions.SectionName)
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBServiceManagementOptions();
            config?.GetSection(sectionName)?.Bind(options);
            return AddDBServiceManagement(services, options);
        }

        /// <summary>
        /// 添加数据库服务管理到依赖注入容器（使用默认 Repository 实现，从配置读取，并支持额外配置）
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托（在配置文件配置之后执行）</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称，默认为 "DBServiceManagement"</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBServiceManagement(this IServiceCollection services, Action<DBServiceManagementOptions> optionsAction, IConfiguration configuration = null, string sectionName = DBServiceManagementOptions.SectionName)
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBServiceManagementOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return AddDBServiceManagement(services, options);
        }

        /// <summary>
        /// 添加数据库服务管理到依赖注入容器（使用自定义仓储实现）
        /// </summary>
        /// <typeparam name="TServiceRegistryRepository">服务注册仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBServiceManagement<TServiceRegistryRepository>(
            this IServiceCollection services, 
            DBServiceManagementOptions options)
            where TServiceRegistryRepository : class, IServiceRegistryRepository
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();

            services.AddSingleton(options);
            services.AddSingleton<IServiceRegistryRepository, TServiceRegistryRepository>();
            services.AddSingleton<IServiceManagement, DBServiceManagement>();
            services.AddSingleton<DBServiceManagement>();

            return services;
        }

        /// <summary>
        /// 添加数据库服务管理到依赖注入容器（使用自定义仓储实现，从配置读取）
        /// </summary>
        /// <typeparam name="TServiceRegistryRepository">服务注册仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBServiceManagement<TServiceRegistryRepository>(
            this IServiceCollection services, 
            Action<DBServiceManagementOptions> optionsAction = null, 
            IConfiguration configuration = null, 
            string sectionName = DBServiceManagementOptions.SectionName)
            where TServiceRegistryRepository : class, IServiceRegistryRepository
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBServiceManagementOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return AddDBServiceManagement<TServiceRegistryRepository>(services, options);
        }

        /// <summary>
        /// 添加数据库服务管理到依赖注入容器（使用自定义仓储实现，包括 Leader 选举仓储）
        /// </summary>
        /// <typeparam name="TServiceRegistryRepository">服务注册仓储实现类型</typeparam>
        /// <typeparam name="TLeaderElectionRepository">Leader 选举仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="options">配置选项</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBServiceManagement<TServiceRegistryRepository, TLeaderElectionRepository>(
            this IServiceCollection services, 
            DBServiceManagementOptions options)
            where TServiceRegistryRepository : class, IServiceRegistryRepository
            where TLeaderElectionRepository : class, ILeaderElectionRepository
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();

            services.AddSingleton(options);
            services.AddSingleton<IServiceRegistryRepository, TServiceRegistryRepository>();
            services.AddSingleton<ILeaderElectionRepository, TLeaderElectionRepository>();
            services.AddSingleton<IServiceManagement, DBServiceManagement>();
            services.AddSingleton<DBServiceManagement>();

            return services;
        }

        /// <summary>
        /// 添加数据库服务管理到依赖注入容器（使用自定义仓储实现，包括 Leader 选举仓储，从配置读取）
        /// </summary>
        /// <typeparam name="TServiceRegistryRepository">服务注册仓储实现类型</typeparam>
        /// <typeparam name="TLeaderElectionRepository">Leader 选举仓储实现类型</typeparam>
        /// <param name="services">服务集合</param>
        /// <param name="optionsAction">额外配置委托</param>
        /// <param name="configuration">配置，为 null 时使用 AppConfig.Configuration</param>
        /// <param name="sectionName">配置节名称</param>
        /// <returns>服务集合</returns>
        public static IServiceCollection AddDBServiceManagement<TServiceRegistryRepository, TLeaderElectionRepository>(
            this IServiceCollection services, 
            Action<DBServiceManagementOptions> optionsAction = null, 
            IConfiguration configuration = null, 
            string sectionName = DBServiceManagementOptions.SectionName)
            where TServiceRegistryRepository : class, IServiceRegistryRepository
            where TLeaderElectionRepository : class, ILeaderElectionRepository
        {
            var config = configuration ?? AppConfig.Configuration;
            var options = new DBServiceManagementOptions();
            config?.GetSection(sectionName)?.Bind(options);
            optionsAction?.Invoke(options);
            return AddDBServiceManagement<TServiceRegistryRepository, TLeaderElectionRepository>(services, options);
        }

        #endregion
    }
}
