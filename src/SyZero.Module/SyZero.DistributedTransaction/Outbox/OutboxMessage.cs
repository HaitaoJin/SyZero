using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.DistributedTransaction
{
    /// <summary>
    /// Outbox 消息：业务事务完成时与业务数据**同库同事务**写入；后台投递器再异步发布到事件总线。
    /// </summary>
    public class OutboxMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string EventName { get; set; } = string.Empty;
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAtUtc { get; set; }
        public int RetryCount { get; set; }
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Outbox 存储抽象。业务可基于 EF / SqlSugar / MongoDB 实现真正的事务一致性。
    /// </summary>
    public interface IOutboxStore
    {
        Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(int batchSize, CancellationToken cancellationToken = default);

        Task MarkPublishedAsync(string id, CancellationToken cancellationToken = default);

        Task MarkFailedAsync(string id, string error, CancellationToken cancellationToken = default);
    }
}
