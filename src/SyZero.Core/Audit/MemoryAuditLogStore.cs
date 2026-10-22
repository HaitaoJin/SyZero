using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero
{
    /// <summary>
    /// 内存审计日志存储，仅供开发/测试使用，不持久化。
    /// </summary>
    public class MemoryAuditLogStore : IAuditLogStore, ISingletonDependency
    {
        private readonly ConcurrentQueue<AuditLogEntry> _entries = new ConcurrentQueue<AuditLogEntry>();
        private const int MaxKeep = 10_000;

        /// <inheritdoc/>
        public Task SaveAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
        {
            _entries.Enqueue(entry);
            // 简易容量保护，超过 MaxKeep 丢弃最早的。
            while (_entries.Count > MaxKeep && _entries.TryDequeue(out _)) { }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IReadOnlyList<AuditLogEntry>> QueryAsync(int take = 100, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AuditLogEntry> result = _entries
                .Reverse()
                .Take(take)
                .ToList();
            return Task.FromResult(result);
        }
    }
}
