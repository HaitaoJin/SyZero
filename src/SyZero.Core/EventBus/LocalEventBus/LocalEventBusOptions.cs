using System;
using System.IO;

namespace SyZero.EventBus.LocalEventBus
{
    /// <summary>
    /// 本地事件总线配置选项
    /// </summary>
    public class LocalEventBusOptions
    {
        /// <summary>
        /// 配置节名称
        /// </summary>
        public const string SectionName = "LocalEventBus";

        /// <summary>
        /// 是否启用文件持久化
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableFilePersistence { get; set; } = true;

        /// <summary>
        /// 订阅数据文件路径
        /// <para>默认值: %TEMP%/syzero_eventbus_subscriptions.json</para>
        /// </summary>
        public string SubscriptionFilePath { get; set; }

        /// <summary>
        /// 事件数据文件路径（用于持久化待处理事件）
        /// <para>默认值: %TEMP%/syzero_eventbus_events.json</para>
        /// </summary>
        public string EventFilePath { get; set; }

        /// <summary>
        /// 是否启用文件监听（多进程同步订阅信息）
        /// <para>默认值: true</para>
        /// </summary>
        public bool EnableFileWatcher { get; set; } = true;

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
        /// 死信队列文件路径
        /// <para>默认值: %TEMP%/syzero_eventbus_deadletter.json</para>
        /// </summary>
        public string DeadLetterFilePath { get; set; }

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
        /// 最大事件队列长度
        /// <para>默认值: 10000</para>
        /// </summary>
        public int MaxEventQueueLength { get; set; } = 10000;

        /// <summary>
        /// 获取订阅文件路径
        /// </summary>
        public string GetSubscriptionFilePath()
        {
            if (!string.IsNullOrWhiteSpace(SubscriptionFilePath))
            {
                return SubscriptionFilePath;
            }

            var tempPath = Path.GetTempPath();
            return Path.Combine(tempPath, "syzero_eventbus_subscriptions.json");
        }

        /// <summary>
        /// 获取事件文件路径
        /// </summary>
        public string GetEventFilePath()
        {
            if (!string.IsNullOrWhiteSpace(EventFilePath))
            {
                return EventFilePath;
            }

            var tempPath = Path.GetTempPath();
            return Path.Combine(tempPath, "syzero_eventbus_events.json");
        }

        /// <summary>
        /// 获取死信队列文件路径
        /// </summary>
        public string GetDeadLetterFilePath()
        {
            if (!string.IsNullOrWhiteSpace(DeadLetterFilePath))
            {
                return DeadLetterFilePath;
            }

            var tempPath = Path.GetTempPath();
            return Path.Combine(tempPath, "syzero_eventbus_deadletter.json");
        }
    }
}
