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

        public Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(int batchSize, string lockOwner, System.TimeSpan lease, CancellationToken cancellationToken = default)
        {
            var now = System.DateTime.UtcNow;
            var claimed = new List<OutboxMessage>();
            foreach (var message in _messages.Values.Where(x => x.PublishedAtUtc == null && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now)).OrderBy(x => x.CreatedAtUtc))
            {
                lock (message)
                {
                    if (message.PublishedAtUtc != null || message.LockedUntilUtc > now) continue;
                    message.LockOwner = lockOwner;
                    message.LockedUntilUtc = now.Add(lease);
                    claimed.Add(message);
                }
                if (claimed.Count >= batchSize) break;
            }
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(claimed);
        }

        public Task MarkPublishedAsync(string id, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var msg))
            {
                msg.PublishedAtUtc = System.DateTime.UtcNow;
                msg.LockOwner = null;
                msg.LockedUntilUtc = null;
            }
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(string id, string error, CancellationToken cancellationToken = default)
        {
            if (_messages.TryGetValue(id, out var msg))
            {
                msg.RetryCount++;
                msg.LastError = error;
                msg.LockOwner = null;
                msg.LockedUntilUtc = null;
            }
            return Task.CompletedTask;
        }
    }
}
