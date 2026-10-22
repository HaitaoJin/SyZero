using System;

namespace SyZero.Service.DBServiceManagement
{
    /// <summary>
    /// 数据库服务管理配置选项
    /// </summary>
    public class DBServiceManagementOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "DBServiceManagement";

        /// <summary>
        /// 数据库表名
        /// <para>默认值: "sys_service_registry"</para>
        /// </summary>
        public string TableName { get; set; } = "sys_service_registry";

        /// <summary>
        /// 缓存过期时间（秒）
        /// <para>默认值: 30</para>
        /// </summary>
        public int CacheExpirationSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用健康检查
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableHealthCheck { get; set; } = true;

        /// <summary>
        /// 健康检查间隔（秒）
        /// <para>默认值: 30</para>
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// 健康检查超时时间（秒）
        /// <para>默认值: 5</para>
        /// </summary>
        public int HealthCheckTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// 服务实例过期时间（秒），超过此时间未心跳的实例将被标记为不健康
        /// <para>默认值: 30</para>
        /// </summary>
        public int ServiceExpireSeconds { get; set; } = 30;

        /// <summary>
        /// 是否自动清理过期服务
        /// <para>默认值: true</para>
        /// </summary>
        public bool AutoCleanExpiredServices { get; set; } = true;

        /// <summary>
        /// 自动清理间隔（秒）
        /// <para>默认值: 300（5分钟）</para>
        /// </summary>
        public int AutoCleanIntervalSeconds { get; set; } = 300;

        /// <summary>
        /// 服务实例清理时间（秒），超过此时间未心跳的实例将被自动移除
        /// <para>默认值: 600（10分钟），应大于 ServiceExpireSeconds</para>
        /// </summary>
        public int ServiceCleanSeconds { get; set; } = 600;

        /// <summary>
        /// 是否启用 Leader 选举（多实例时只有 Leader 执行健康检查和清理）
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableLeaderElection { get; set; } = true;

        /// <summary>
        /// Leader 锁过期时间（秒）
        /// <para>默认值: 30</para>
        /// </summary>
        public int LeaderLockExpireSeconds { get; set; } = 30;

        /// <summary>
        /// Leader 锁续期间隔（秒）
        /// <para>默认值: 10，应小于 LeaderLockExpireSeconds</para>
        /// </summary>
        public int LeaderLockRenewIntervalSeconds { get; set; } = 10;

        /// <summary>
        /// 验证配置
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(TableName))
            {
                throw new ArgumentException("TableName 不能为空");
            }

            if (CacheExpirationSeconds < 0)
            {
                throw new ArgumentException("CacheExpirationSeconds 必须大于等于 0");
            }

            if (HealthCheckIntervalSeconds < 1)
            {
                throw new ArgumentException("HealthCheckIntervalSeconds 必须大于 0");
            }

            if (ServiceExpireSeconds < HealthCheckIntervalSeconds)
            {
                throw new ArgumentException("ServiceExpireSeconds 必须大于 HealthCheckIntervalSeconds");
            }
        }
    }
}
