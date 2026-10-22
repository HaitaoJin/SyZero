using System;
using Microsoft.Extensions.Options;
using SyZero.Cache;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 基于 <see cref="ICache"/> 抽象的登录失败追踪器实现。
    /// 宿主只要注册了任意 <see cref="ICache"/>（例如 <c>SyZero.Redis</c> 提供的 Redis 实现），
    /// 即可在多实例网关之间共享失败计数与锁定状态。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 计数采用 Get → 修改 → Set 的乐观写入流程，对登录限流这类低频写入场景足够；
    /// 跨实例并发可能有极小概率丢失 1 次失败计数，但不会破坏锁定状态本身。
    /// </para>
    /// <para>键名格式：<c>{KeyPrefix}{client-key}</c>，默认前缀 <c>gw-admin:rate-limit:</c>。</para>
    /// </remarks>
    public sealed class CachedFailedLoginTracker : IFailedLoginTracker
    {
        /// <summary>缓存键前缀，所有失败计数键共享。</summary>
        public const string KeyPrefix = "gw-admin:rate-limit:";

        private readonly ICache _cache;
        private readonly IOptionsMonitor<GatewayAdminRateLimitOptions> _options;

        public CachedFailedLoginTracker(ICache cache, IOptionsMonitor<GatewayAdminRateLimitOptions> options)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public int MaxFailures => _options.CurrentValue.EffectiveMaxFailures;

        /// <inheritdoc />
        public int GetLockoutSeconds(string clientKey)
        {
            if (string.IsNullOrEmpty(clientKey)) return 0;
            var entry = _cache.Get<RateLimitEntry>(BuildKey(clientKey));
            if (entry == null || entry.LockoutUntilUtc == default) return 0;
            var remain = (int)Math.Ceiling((entry.LockoutUntilUtc - DateTime.UtcNow).TotalSeconds);
            return remain > 0 ? remain : 0;
        }

        /// <inheritdoc />
        public int RecordFailure(string clientKey)
        {
            if (string.IsNullOrEmpty(clientKey)) return 0;
            var opts = _options.CurrentValue;
            var maxFailures = opts.EffectiveMaxFailures;
            var lockoutMinutes = opts.EffectiveLockoutMinutes;
            var windowMinutes = opts.EffectiveWindowMinutes;
            var now = DateTime.UtcNow;

            var key = BuildKey(clientKey);
            var entry = _cache.Get<RateLimitEntry>(key) ?? new RateLimitEntry();

            // 当前未处于锁定，且窗口已过 → 重置计数。
            if (entry.LockoutUntilUtc <= now
                && entry.FirstFailureUtc != default
                && (now - entry.FirstFailureUtc).TotalMinutes >= windowMinutes)
            {
                entry = new RateLimitEntry();
            }

            if (entry.FirstFailureUtc == default) entry.FirstFailureUtc = now;
            entry.Count++;
            if (entry.Count >= maxFailures)
            {
                entry.LockoutUntilUtc = now.AddMinutes(lockoutMinutes);
            }

            // TTL 取「锁定剩余」与「窗口剩余」中较大者，至少 60 秒，避免提前过期丢状态。
            var ttlSeconds = Math.Max(60, (int)Math.Ceiling(Math.Max(
                (entry.LockoutUntilUtc - now).TotalSeconds,
                (entry.FirstFailureUtc.AddMinutes(windowMinutes) - now).TotalSeconds)));
            _cache.Set(key, entry, ttlSeconds);
            return entry.Count;
        }

        /// <inheritdoc />
        public void Reset(string clientKey)
        {
            if (string.IsNullOrEmpty(clientKey)) return;
            _cache.Remove(BuildKey(clientKey));
        }

        /// <inheritdoc />
        public int GetFailureCount(string clientKey)
        {
            if (string.IsNullOrEmpty(clientKey)) return 0;
            var entry = _cache.Get<RateLimitEntry>(BuildKey(clientKey));
            if (entry == null) return 0;
            // 锁定中或窗口内 → 返回；否则窗口已过且未锁定视为 0
            var now = DateTime.UtcNow;
            if (entry.LockoutUntilUtc > now) return entry.Count;
            if (entry.FirstFailureUtc != default
                && (now - entry.FirstFailureUtc).TotalMinutes >= _options.CurrentValue.EffectiveWindowMinutes)
            {
                return 0;
            }
            return entry.Count;
        }

        private static string BuildKey(string clientKey) => KeyPrefix + clientKey;

        /// <summary>
        /// 缓存中持久化的失败计数条目（公开仅为了序列化兼容，外部不应直接构造）。
        /// </summary>
        public sealed class RateLimitEntry
        {
            public int Count { get; set; }
            public DateTime FirstFailureUtc { get; set; }
            public DateTime LockoutUntilUtc { get; set; }
        }
    }
}
