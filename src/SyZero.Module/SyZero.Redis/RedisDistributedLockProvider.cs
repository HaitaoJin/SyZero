using System;
using System.Threading;
using System.Threading.Tasks;
using FreeRedis;
using SyZero.DistributedLock;

namespace SyZero.Redis
{
    /// <summary>
    /// 基于 Redis <c>SET NX PX</c> + Lua 释放脚本的 <see cref="IDistributedLockProvider"/> 实现。
    /// 多实例部署的推荐选择。
    /// </summary>
    public class RedisDistributedLockProvider : IDistributedLockProvider
    {
        private const string ReleaseScript =
            "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";

        // 等待轮询间隔（毫秒）
        private const int RetryIntervalMs = 60;

        private readonly RedisClient _redis;

        public RedisDistributedLockProvider(RedisClient redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        }

        public async Task<IAsyncDisposable?> AcquireAsync(
            string key,
            TimeSpan expiration,
            TimeSpan waitTimeout,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("锁名不能为空", nameof(key));
            if (expiration <= TimeSpan.Zero) expiration = TimeSpan.FromSeconds(10);

            var token = Guid.NewGuid().ToString("N");
            var deadline = waitTimeout <= TimeSpan.Zero ? DateTime.UtcNow : DateTime.UtcNow + waitTimeout;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // FreeRedis: SET key value EX seconds NX；NX 失败返回 null，否则 "OK"
                var resp = _redis.Set(key, token, expiration, nx: true, xx: false, keepTtl: false, get: false);
                if (string.Equals(resp, "OK", StringComparison.OrdinalIgnoreCase))
                    return new RedisLockReleaser(_redis, key, token);

                if (waitTimeout <= TimeSpan.Zero || DateTime.UtcNow >= deadline) return null;

                try
                {
                    await Task.Delay(RetryIntervalMs, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return null;
                }
            }
        }

        private sealed class RedisLockReleaser : IAsyncDisposable
        {
            private readonly RedisClient _redis;
            private readonly string _key;
            private readonly string _token;
            private int _released;

            public RedisLockReleaser(RedisClient redis, string key, string token)
            {
                _redis = redis;
                _key = key;
                _token = token;
            }

            public ValueTask DisposeAsync()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    try { _redis.Eval(ReleaseScript, new[] { _key }, _token); }
                    catch { /* 释放失败不抛异常，过期保护兜底 */ }
                }
                return default;
            }
        }
    }
}
