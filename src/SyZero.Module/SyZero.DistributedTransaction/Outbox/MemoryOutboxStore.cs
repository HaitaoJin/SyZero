using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.DistributedTransaction
{
    /// <summary>
    /// 默认进程内 <see cref="IOutboxStore"/> 实现，仅适合演示与单元测试。
    /// 生产请改用 EF Core / SqlSugar / MongoDB 实现，并把 EnqueueAsync 加入业务事务。
    /// </summary>
    public class MemoryOutboxStore : IOutboxStore, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, OutboxMessage> _messages = new ConcurrentDictionary<string, OutboxMessage>();

        public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            _messages[message.Id] = message;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            var pending = _messages.Values
                .Where(m => m.PublishedAtUtc == null)
                .OrderBy(m => m.CreatedAtUtc)
                .Take(batchSize)
                .ToList();
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(pending);
        }

        public Task MarkPublishedAsync(string id, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var msg))
            {
                msg.PublishedAtUtc = System.DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(string id, string error, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var msg))
            {
                msg.RetryCount++;
                msg.LastError = error;
            }
            return Task.CompletedTask;
        }
    }
}
