using System;

namespace SyZero.EventBus.DBEventBus
{
    /// <summary>
    /// 数据库事件总线配置选项
    /// </summary>
    public class DBEventBusOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "DBEventBus";

        /// <summary>
        /// 事件订阅表名
        /// <para>默认值: "sys_event_subscription"</para>
        /// </summary>
        public string SubscriptionTableName { get; set; } = "sys_event_subscription";

        /// <summary>
        /// 事件队列表名
        /// <para>默认值: "sys_event_queue"</para>
        /// </summary>
        public string EventQueueTableName { get; set; } = "sys_event_queue";

        /// <summary>
        /// 死信队列表名
        /// <para>默认值: "sys_event_deadletter"</para>
        /// </summary>
        public string DeadLetterTableName { get; set; } = "sys_event_deadletter";

        /// <summary>
        /// 缓存过期时间（秒）
        /// <para>默认值: 30</para>
        /// </summary>
        public int CacheExpirationSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用异步事件处理
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableAsync { get; set; } = true;

        /// <summary>
        /// 事件处理超时时间（秒）
        /// <para>默认值: 30</para>
        /// </summary>
        public int EventHandlerTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用事件重试机制
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableRetry { get; set; } = true;

        /// <summary>
        /// 事件重试次数
        /// <para>默认值: 3</para>
        /// </summary>
        public int RetryCount { get; set; } = 3;

        /// <summary>
        /// 事件重试间隔（秒）
        /// <para>默认值: 5</para>
        /// </summary>
        public int RetryIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// 是否启用死信队列（失败事件存储）
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableDeadLetterQueue { get; set; } = true;

        /// <summary>
        /// 是否自动清理过期事件
        /// <para>默认值: true</para>
        /// </summary>
        public bool AutoCleanExpiredEvents { get; set; } = true;

        /// <summary>
        /// 自动清理间隔（秒）
        /// <para>默认值: 300（5分钟）</para>
        /// </summary>
        public int AutoCleanIntervalSeconds { get; set; } = 300;

        /// <summary>
        /// 事件过期时间（秒），超过此时间未处理的事件将被清理
        /// <para>默认值: 86400（24小时）</para>
        /// </summary>
        public int EventExpireSeconds { get; set; } = 86400;

        /// <summary>
        /// 事件处理间隔（秒），后台任务检查待处理事件的间隔
        /// <para>默认值: 5</para>
        /// </summary>
        public int EventProcessIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// 每次处理的最大事件数
        /// <para>默认值: 100</para>
        /// </summary>
        public int MaxEventsPerBatch { get; set; } = 100;

        /// <summary>
        /// 是否启用 Leader 选举（只有 Leader 实例执行事件处理）
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
        /// <para>默认值: 10</para>
        /// </summary>
        public int LeaderLockRenewIntervalSeconds { get; set; } = 10;
    }
}
