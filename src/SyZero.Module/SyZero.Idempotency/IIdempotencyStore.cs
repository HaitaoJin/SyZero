using System;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Idempotency
{
    /// <summary>
    /// 已缓存响应的轻量快照。
    /// </summary>
    public class IdempotencyEntry
    {
        public int StatusCode { get; set; }
        public string? ContentType { get; set; }
        public byte[]? Body { get; set; }
    }

    /// <summary>
    /// 幂等键存储抽象。
    /// </summary>
    public interface IIdempotencyStore
    {
        /// <summary>
        /// 尝试占用一个 key（首次调用返回 true，之后调用返回 false 直到 key 过期或被填充）。
        /// </summary>
        Task<bool> TryReserveAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 写入已经完成的响应快照（覆盖此前的占位）。
        /// </summary>
        Task SaveAsync(string key, IdempotencyEntry entry, TimeSpan expiration, CancellationToken cancellationToken = default);

        /// <summary>
        /// 读取响应快照；若仅占位未完成返回 null（中间件应回 409 / 等待）。
        /// </summary>
        Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
    }
}
