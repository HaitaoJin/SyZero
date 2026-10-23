using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 登录失败限流：按客户端 IP 在内存中累计失败次数，超过阈值则在窗口期内拒绝登录。
    /// 进程重启即清空。多实例部署需要更强一致性时可由宿主替换实现。
    /// </summary>
    public interface IFailedLoginTracker
    {
        /// <summary>
        /// 检查是否被锁定。锁定中返回剩余秒数（&gt; 0）；未锁定返回 0。
        /// </summary>
        int GetLockoutSeconds(string clientKey);

        /// <summary>记录一次失败，返回累计失败次数。达到阈值时自动开启锁定窗口。</summary>
        int RecordFailure(string clientKey);

        /// <summary>登录成功后清除该 IP 的失败计数。</summary>
        void Reset(string clientKey);

        /// <summary>当前生效的最大失败次数（用于响应中给出 remaining 提示）。</summary>
        int MaxFailures { get; }

        /// <summary>获取该 client 当前累计的失败次数（用于判断是否需要展示验证码）。</summary>
        int GetFailureCount(string clientKey);
    }

    public sealed class FailedLoginTracker : IFailedLoginTracker
    {
        private sealed class Entry
        {
            public int Count { get; init; }
            public DateTimeOffset FirstFailureUtc { get; init; }
            public DateTimeOffset LockoutUntilUtc { get; init; }
        }

        private readonly ConcurrentDictionary<string, Entry> _entries = new();
        private readonly IOptionsMonitor<GatewayAdminRateLimitOptions> _options;

        public FailedLoginTracker(IOptionsMonitor<GatewayAdminRateLimitOptions> options)
        {
            _options = options;
        }

        public int MaxFailures => _options.CurrentValue.EffectiveMaxFailures;

        private TimeSpan LockoutDuration =>
            TimeSpan.FromMinutes(_options.CurrentValue.EffectiveLockoutMinutes);

        private TimeSpan FailureCountWindow =>
            TimeSpan.FromMinutes(_options.CurrentValue.EffectiveWindowMinutes);

        public int GetLockoutSeconds(string clientKey)
        {
            if (string.IsNullOrEmpty(clientKey) || !_entries.TryGetValue(clientKey, out var e))
            {
                return 0;
            }
            var now = DateTimeOffset.UtcNow;
            if (e.LockoutUntilUtc > now)
            {
                return (int)Math.Ceiling((e.LockoutUntilUtc - now).TotalSeconds);
            }
            return 0;
        }

        public int RecordFailure(string clientKey)
        {
            if (string.IsNullOrEmpty(clientKey)) clientKey = "unknown";
            var now = DateTimeOffset.UtcNow;
            var maxFailures = MaxFailures;
            var window = FailureCountWindow;
            var lockoutDuration = LockoutDuration;

            var entry = _entries.AddOrUpdate(
                clientKey,
                _ => new Entry
                {
                    Count = 1,
                    FirstFailureUtc = now,
                    LockoutUntilUtc = maxFailures <= 1 ? now + lockoutDuration : default,
                },
                (_, existing) =>
                {
                    // 失败窗口超时则重置计数。
                    if (now - existing.FirstFailureUtc > window
                        && existing.LockoutUntilUtc <= now)
                    {
                        return new Entry
                        {
                            Count = 1,
                            FirstFailureUtc = now,
                            LockoutUntilUtc = maxFailures <= 1 ? now + lockoutDuration : default,
                        };
                    }
                    var count = existing.Count + 1;
                    return new Entry
                    {
                        Count = count,
                        FirstFailureUtc = existing.FirstFailureUtc,
                        LockoutUntilUtc = count >= maxFailures
                            ? now + lockoutDuration
                            : existing.LockoutUntilUtc,
                    };
                });
            return entry.Count;
        }

        public void Reset(string clientKey)
        {
            if (!string.IsNullOrEmpty(clientKey))
            {
                _entries.TryRemove(clientKey, out _);
            }
        }

        public int GetFailureCount(string clientKey)
        {
            if (string.IsNullOrEmpty(clientKey) || !_entries.TryGetValue(clientKey, out var e)) return 0;
            // 窗口超时且未锁定 → 视为 0
            if (e.LockoutUntilUtc <= DateTimeOffset.UtcNow
                && DateTimeOffset.UtcNow - e.FirstFailureUtc > FailureCountWindow) return 0;
            return e.Count;
        }
    }
}
