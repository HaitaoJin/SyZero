using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Cache
{
    /// <summary>
    /// 多级缓存抽象（L1 = 进程内内存，L2 = 分布式缓存）。
    /// 内置缓存穿透（空值缓存）/击穿（SingleFlight 单飞）/雪崩（随机过期）保护。
    /// </summary>
    public interface IMultiLevelCache
    {
        /// <summary>
        /// 获取缓存；不存在则调用 <paramref name="factory"/> 加载并写入两级缓存。
        /// </summary>
        /// <typeparam name="T">值类型。</typeparam>
        /// <param name="key">缓存键。</param>
        /// <param name="factory">数据加载委托；返回 null 时按穿透保护策略缓存空值（如启用）。</param>
        /// <param name="absoluteExpiration">L2 绝对过期时间，会自动叠加随机抖动避免雪崩。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 显式写入两级缓存。
        /// </summary>
        Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 同时移除 L1 与 L2。
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    }
}
