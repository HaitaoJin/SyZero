namespace SyZero.Cache
{
    /// <summary>
    /// 多级缓存配置。
    /// </summary>
    public class MultiLevelCacheOptions
    {
        public const string SectionName = "SyZeroMultiLevelCache";

        /// <summary>
        /// L1（进程内）默认过期时间（秒），用于把 L2 的长过期时间在本地短暂缓存。
        /// </summary>
        public int L1ExpirationSeconds { get; set; } = 30;

        /// <summary>
        /// 是否启用空值缓存（穿透保护）；防止恶意请求大量穿透到底层存储。
        /// </summary>
        public bool EnableNullCache { get; set; } = true;

        /// <summary>
        /// 空值缓存过期时间（秒）。
        /// </summary>
        public int NullCacheSeconds { get; set; } = 30;

        /// <summary>
        /// 雪崩防护：在 L2 过期时间上随机叠加的抖动比例（0~1），实际过期时间 = base * (1 + rand(0, jitter))。
        /// </summary>
        public double JitterRatio { get; set; } = 0.1;
    }
}
