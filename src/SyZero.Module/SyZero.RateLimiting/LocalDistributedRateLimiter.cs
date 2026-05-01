using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.RateLimiting
{
    /// <summary>
    /// 进程内分布式限流实现：基于固定窗口计数器（key + 窗口起点 → 计数）。
    /// 仅适用于单实例或粘性会话场景；多实例请使用 Redis 实现。
    /// </summary>
    public class LocalDistributedRateLimiter : IDistributedRateLimiter, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, Counter> _counters = new ConcurrentDictionary<string, Counter>();

        /// <inheritdoc/>
        public Task<bool> TryAcquireAsync(string key, int permitLimit, int window, CancellationToken cancellationToken = default)
        {
            if (permitLimit <= 0 || window <= 0)
            {
                return Task.FromResult(false);
            }

            var nowTicks = DateTime.UtcNow.Ticks;
            var windowTicks = TimeSpan.FromSeconds(window).Ticks;
            var windowStart = nowTicks - (nowTicks % windowTicks);

            var counter = _counters.AddOrUpdate(key,
                _ => new Counter { WindowStart = windowStart, Count = 0 },
                (_, existing) =>
                {
                    if (existing.WindowStart != windowStart)
                    {
                        existing.WindowStart = windowStart;
                        Interlocked.Exchange(ref existing.Count, 0);
                    }
                    return existing;
                });

            var current = Interlocked.Increment(ref counter.Count);
            return Task.FromResult(current <= permitLimit);
        }

        private sealed class Counter
        {
            public long WindowStart;
            public long Count;
        }
    }
}
