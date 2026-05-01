using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.DistributedLock
{
    /// <summary>
    /// 分布式锁提供者。<see cref="AcquireAsync"/> 成功返回一个 <see cref="IAsyncDisposable"/>，
    /// Dispose 时释放锁；获取超时返回 <c>null</c>（调用方按业务决定 fail-fast 还是重试）。
    /// </summary>
    public interface IDistributedLockProvider
    {
        /// <param name="key">锁名（建议带业务前缀，如 <c>order:1001</c>）。</param>
        /// <param name="expiration">锁的过期时间，进程崩溃时由后端自动释放。</param>
        /// <param name="waitTimeout">获取锁的最长等待时间。<see cref="TimeSpan.Zero"/> 表示不等待，直接尝试一次。</param>
        Task<IAsyncDisposable?> AcquireAsync(
            string key,
            TimeSpan expiration,
            TimeSpan waitTimeout,
            CancellationToken cancellationToken = default);
    }
}
