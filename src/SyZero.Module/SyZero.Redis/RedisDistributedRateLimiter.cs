using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FreeRedis;
using SyZero.RateLimiting;

namespace SyZero.Redis;

/// <summary>使用 Redis 有序集合和服务端时间实现的原子滑动窗口限流器。</summary>
public sealed class RedisDistributedRateLimiter(RedisClient redis) : IDistributedRateLimiter
{
    private const string Script = "local t=redis.call('TIME'); local now=t[1]*1000+math.floor(t[2]/1000); local start=now-tonumber(ARGV[1]); redis.call('ZREMRANGEBYSCORE',KEYS[1],0,start); local count=redis.call('ZCARD',KEYS[1]); if count>=tonumber(ARGV[2]) then return 0 end; redis.call('ZADD',KEYS[1],now,ARGV[3]); redis.call('PEXPIRE',KEYS[1],ARGV[1]); return 1";

    public Task<bool> TryAcquireAsync(string key, int permitLimit, int window, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(permitLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(window);
        var result = redis.Eval(Script, new[] { $"syzero:rate:{key}" },
            (window * 1000L).ToString(CultureInfo.InvariantCulture),
            permitLimit.ToString(CultureInfo.InvariantCulture),
            Guid.NewGuid().ToString("N"));
        return Task.FromResult(Convert.ToInt64(result, CultureInfo.InvariantCulture) == 1);
    }
}
