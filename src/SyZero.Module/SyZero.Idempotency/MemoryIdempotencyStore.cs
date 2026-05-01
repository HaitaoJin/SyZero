using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Idempotency
{
    /// <summary>
    /// 进程内 <see cref="IIdempotencyStore"/> 默认实现：仅适合单实例。
    /// 多实例请使用 SyZero.Redis 中的 RedisIdempotencyStore（基于 SET NX）。
    /// </summary>
    public class MemoryIdempotencyStore : IIdempotencyStore, ISingletonDependency
    {
        private class Slot
        {
            public DateTime ExpiresAtUtc;
            public IdempotencyEntry? Entry;
        }

        private readonly ConcurrentDictionary<string, Slot> _slots = new ConcurrentDictionary<string, Slot>();

        public Task<bool> TryReserveAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            CleanExpired();
            var slot = new Slot { ExpiresAtUtc = DateTime.UtcNow.Add(expiration) };
            var added = _slots.TryAdd(key, slot);
            return Task.FromResult(added);
        }

        public Task SaveAsync(string key, IdempotencyEntry entry, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            _slots[key] = new Slot
            {
                ExpiresAtUtc = DateTime.UtcNow.Add(expiration),
                Entry = entry
            };
            return Task.CompletedTask;
        }

        public Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            if (_slots.TryGetValue(key, out var slot))
            {
                if (slot.ExpiresAtUtc < DateTime.UtcNow)
                {
                    _slots.TryRemove(key, out _);
                    return Task.FromResult<IdempotencyEntry?>(null);
                }
                return Task.FromResult(slot.Entry);
            }
            return Task.FromResult<IdempotencyEntry?>(null);
        }

        private void CleanExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _slots)
            {
                if (kv.Value.ExpiresAtUtc < now)
                {
                    _slots.TryRemove(kv.Key, out _);
                }
            }
        }
    }
}
