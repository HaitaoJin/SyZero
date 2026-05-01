using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.DistributedLock
{
    /// <summary>
    /// 进程内默认实现：基于 <see cref="SemaphoreSlim"/> 的 Local 锁。仅适用于单实例部署 / 测试。
    /// 多实例请替换为基于 Redis / DB 的分布式实现。
    /// </summary>
    public class LocalDistributedLockProvider : IDistributedLockProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        public async Task<IAsyncDisposable?> AcquireAsync(
            string key,
            TimeSpan expiration,
            TimeSpan waitTimeout,
            CancellationToken cancellationToken = default)
        {
            var sem = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            var acquired = waitTimeout <= TimeSpan.Zero
                ? sem.Wait(0, cancellationToken)
                : await sem.WaitAsync(waitTimeout, cancellationToken).ConfigureAwait(false);

            if (!acquired) return null;

            // 过期保护：到期自动释放，避免持有者死锁。
            CancellationTokenSource? cts = null;
            if (expiration > TimeSpan.Zero)
            {
                cts = new CancellationTokenSource(expiration);
                cts.Token.Register(() =>
                {
                    try { sem.Release(); } catch (SemaphoreFullException) { }
                });
            }

            return new Releaser(sem, cts);
        }

        private sealed class Releaser : IAsyncDisposable
        {
            private readonly SemaphoreSlim _sem;
            private readonly CancellationTokenSource? _cts;
            private int _released;

            public Releaser(SemaphoreSlim sem, CancellationTokenSource? cts)
            {
                _sem = sem;
                _cts = cts;
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    try { _sem.Release(); } catch (SemaphoreFullException) { }
                    _cts?.Dispose();
                }
                return default;
            }
        }
    }
}
