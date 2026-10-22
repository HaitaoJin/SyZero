using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SyZero.Dependency;
using SyZero.Serialization;

namespace SyZero.Cache
{
    /// <summary>
    /// 默认多级缓存实现：L1 = <see cref="IMemoryCache"/>，L2 = <see cref="IDistributedCache"/>。
    /// 内置 SingleFlight（防击穿）、空值缓存（防穿透）、随机抖动（防雪崩）。
    /// </summary>
    public class MultiLevelCache : IMultiLevelCache, ISingletonDependency
    {
        // 用于空值缓存的标记
        internal const string NullSentinel = "__SYZERO_NULL__";

        private readonly IMemoryCache _memory;
        private readonly IDistributedCache _distributed;
        private readonly IJsonSerialize _serializer;
        private readonly MultiLevelCacheOptions _options;
        private readonly ILogger<MultiLevelCache>? _logger;

        // SingleFlight：同一个 key 的并发加载只执行一次
        private readonly ConcurrentDictionary<string, Lazy<Task<object?>>> _inflight = new ConcurrentDictionary<string, Lazy<Task<object?>>>();

        public MultiLevelCache(
            IMemoryCache memory,
            IDistributedCache distributed,
            IJsonSerialize serializer,
            IOptions<MultiLevelCacheOptions>? options = null,
            ILogger<MultiLevelCache>? logger = null)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));
            _distributed = distributed ?? throw new ArgumentNullException(nameof(distributed));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _options = options?.Value ?? new MultiLevelCacheOptions();
            _logger = logger;
        }

        public async Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            // L1
            if (_memory.TryGetValue(key, out object? l1))
            {
                return ConvertFrom<T>(l1);
            }

            // L2
            var l2Bytes = await _distributed.GetAsync(key, cancellationToken).ConfigureAwait(false);
            if (l2Bytes != null && l2Bytes.Length > 0)
            {
                var json = Encoding.UTF8.GetString(l2Bytes);
                if (json == NullSentinel)
                {
                    PromoteToL1(key, NullSentinel);
                    return default;
                }

                var value = _serializer.JSONToObject<T>(json);
                PromoteToL1(key, value);
                return value;
            }

            // SingleFlight 加载
            var lazy = _inflight.GetOrAdd(key, _ => new Lazy<Task<object?>>(async () =>
            {
                try
                {
                    var loaded = await factory(cancellationToken).ConfigureAwait(false);
                    await WriteAsync(key, loaded, absoluteExpiration, cancellationToken).ConfigureAwait(false);
                    return loaded;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "MultiLevelCache factory failed for key {Key}", key);
                    throw;
                }
                finally
                {
                    _inflight.TryRemove(key, out Lazy<Task<object?>> _);
                }
            }));

            var result = await lazy.Value.ConfigureAwait(false);
            return ConvertFrom<T>(result);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            await WriteAsync(key, value, absoluteExpiration, cancellationToken).ConfigureAwait(false);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            _memory.Remove(key);
            await _distributed.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }

        private async Task WriteAsync<T>(string key, T value, TimeSpan absoluteExpiration, CancellationToken cancellationToken)
        {
            string json;
            if (value is null)
            {
                if (!_options.EnableNullCache)
                {
                    return;
                }
                json = NullSentinel;
                absoluteExpiration = TimeSpan.FromSeconds(_options.NullCacheSeconds);
            }
            else
            {
                json = _serializer.ObjectToJSON(value) ?? NullSentinel;
            }

            // 雪崩防护：随机抖动
            var jittered = ApplyJitter(absoluteExpiration);
            var bytes = Encoding.UTF8.GetBytes(json);
            await _distributed.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = jittered
            }, cancellationToken).ConfigureAwait(false);

            PromoteToL1(key, value);
        }

        private void PromoteToL1(object? value, string key)
        {
            // overload-only helper not used; kept for clarity
        }

        private void PromoteToL1<T>(string key, T value)
        {
            var l1Ttl = TimeSpan.FromSeconds(Math.Max(1, _options.L1ExpirationSeconds));
            _memory.Set(key, (object?)value, l1Ttl);
        }

        private TimeSpan ApplyJitter(TimeSpan baseTtl)
        {
            if (_options.JitterRatio <= 0)
            {
                return baseTtl;
            }
            var rand = new Random();
            var ratio = 1 + rand.NextDouble() * _options.JitterRatio;
            var ms = baseTtl.TotalMilliseconds * ratio;
            return TimeSpan.FromMilliseconds(ms);
        }

        private static T? ConvertFrom<T>(object? value)
        {
            if (value is null) return default;
            if (value is string s && s == NullSentinel) return default;
            if (value is T typed) return typed;
            return default;
        }
    }
}
