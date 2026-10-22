using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.RateLimiting
{
    /// <summary>
    /// 分布式限流抽象。默认实现 <see cref="LocalDistributedRateLimiter"/> 使用进程内令牌桶，
    /// 多实例部署请改用 SyZero.Redis 提供的 Redis 实现（通过 INCR + EXPIRE 模拟滑动窗口）。
    /// </summary>
    public interface IDistributedRateLimiter
    {
        /// <summary>
        /// 尝试在指定窗口内消费一次令牌。
        /// </summary>
        /// <param name="key">限流维度键，例如 "user:123" / "ip:1.2.3.4" / "api:/orders"。</param>
        /// <param name="permitLimit">窗口内允许的最大次数。</param>
        /// <param name="window">窗口长度（秒）。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>true 表示获得许可，false 表示已超过限制。</returns>
        Task<bool> TryAcquireAsync(string key, int permitLimit, int window, CancellationToken cancellationToken = default);
    }
}
