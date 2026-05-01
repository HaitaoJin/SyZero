using System;
using System.Threading;
using System.Threading.Tasks;
using FreeRedis;
using Microsoft.Extensions.Caching.Distributed;

namespace SyZero.Redis
{
    /// <summary>
    /// 基于 <see cref="RedisClient"/> 的 <see cref="IDistributedCache"/> 实现。
    /// 引入 SyZero.Redis 后，<see cref="SyZero.Cache.IMultiLevelCache"/> 的 L2 自动切换为 Redis。
    /// </summary>
    public class RedisDistributedCache : IDistributedCache
    {
        private readonly RedisClient _client;

        public RedisDistributedCache(RedisClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public byte[]? Get(string key)
        {
            ValidateKey(key);
            return _client.Get<byte[]>(key);
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            ValidateKey(key);
            token.ThrowIfCancellationRequested();
            return await _client.GetAsync<byte[]>(key).ConfigureAwait(false);
        }

        public void Refresh(string key)
        {
            ValidateKey(key);
            _ = _client.Exists(key);
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            ValidateKey(key);
            _client.Del(key);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            ValidateKey(key);
            token.ThrowIfCancellationRequested();
            await _client.DelAsync(key).ConfigureAwait(false);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            ValidateKey(key);
            if (value == null) throw new ArgumentNullException(nameof(value));
            var seconds = ResolveSeconds(options);
            if (seconds > 0)
            {
                _client.Set(key, value, seconds);
            }
            else
            {
                _client.Set(key, value);
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            ValidateKey(key);
            if (value == null) throw new ArgumentNullException(nameof(value));
            token.ThrowIfCancellationRequested();
            var seconds = ResolveSeconds(options);
            if (seconds > 0)
            {
                await _client.SetAsync(key, value, seconds).ConfigureAwait(false);
            }
            else
            {
                await _client.SetAsync(key, value).ConfigureAwait(false);
            }
        }

        private static int ResolveSeconds(DistributedCacheEntryOptions options)
        {
            if (options == null) return 0;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                return (int)options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;
            }
            if (options.AbsoluteExpiration.HasValue)
            {
                var span = options.AbsoluteExpiration.Value - DateTimeOffset.UtcNow;
                return (int)Math.Max(1, span.TotalSeconds);
            }
            if (options.SlidingExpiration.HasValue)
            {
                // FreeRedis 不直接支持滑动；按 sliding 当作绝对处理。
                return (int)options.SlidingExpiration.Value.TotalSeconds;
            }
            return 0;
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
        }
    }
}
