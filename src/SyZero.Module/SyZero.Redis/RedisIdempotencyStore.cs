using System;
using System.Threading;
using System.Threading.Tasks;
using FreeRedis;
using Newtonsoft.Json;
using SyZero.Idempotency;

namespace SyZero.Redis;

/// <summary>跨实例共享的 Redis 幂等响应存储。</summary>
public sealed class RedisIdempotencyStore(RedisClient redis) : IIdempotencyStore
{
    private const string Pending = "__syzero_pending__";
    private const string ReleaseScript = "if redis.call('get', KEYS[1]) == ARGV[1] then return redis.call('del', KEYS[1]) else return 0 end";
    private static string Key(string key) => $"syzero:idempotency:{key}";

    public Task<bool> TryReserveAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
        => Task.FromResult(redis.Set(Key(key), Pending, expiration, nx: true, xx: false, keepTtl: false, get: false) is not null);

    public Task SaveAsync(string key, IdempotencyEntry entry, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        redis.Set(Key(key), JsonConvert.SerializeObject(entry), expiration);
        return Task.CompletedTask;
    }

    public Task<IdempotencyEntry?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = redis.Get(Key(key));
        return Task.FromResult(string.IsNullOrEmpty(value) || value == Pending
            ? null
            : JsonConvert.DeserializeObject<IdempotencyEntry>(value));
    }

    public Task ReleaseAsync(string key, CancellationToken cancellationToken = default)
    {
        redis.Eval(ReleaseScript, new[] { Key(key) }, Pending);
        return Task.CompletedTask;
    }
}
