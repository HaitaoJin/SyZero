namespace SyZero.DistributedTransaction
{
    /// <summary>
    /// Outbox 后台投递配置。
    /// </summary>
    public class OutboxOptions
    {
        public const string SectionName = "SyZeroOutbox";

        /// <summary>
        /// 轮询间隔（秒），默认 2 秒。
        /// </summary>
        public int PollIntervalSeconds { get; set; } = 2;

        /// <summary>
        /// 单批处理消息数量，默认 100。
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// 最大重试次数（达到后保留错误状态等待人工处理），默认 5。
        /// </summary>
        public int MaxRetryCount { get; set; } = 5;
    }
}
