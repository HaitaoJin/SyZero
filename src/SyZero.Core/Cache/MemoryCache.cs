using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Cache
{
    /// <summary>
    /// 基于内存的缓存实现
    /// </summary>
    public class MemoryCache : ICache, ISingletonDependency
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, DateTime> _keys;

        public MemoryCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
            _keys = new ConcurrentDictionary<string, DateTime>();
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exist(string key)
        {
            return _memoryCache.TryGetValue(key, out _);
        }

        /// <summary>
        /// 获取值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            return _memoryCache.TryGetValue(key, out T value) ? value : default;
        }

        /// <summary>
        /// 根据前缀获取所有的key
        /// 例如：pro_*
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public string[] GetKeys(string pattern)
        {
            // 清理过期的key
            CleanExpiredKeys();

            if (string.IsNullOrEmpty(pattern))
            {
                return _keys.Keys.ToArray();
            }

            // 将通配符模式转换为正则表达式
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

            return _keys.Keys.Where(k => regex.IsMatch(k)).ToArray();
        }

        /// <summary>
        /// 重置值
        /// </summary>
        /// <param name="key"></param>
        public void Refresh(string key)
        {
            // MemoryCache 不支持直接刷新，需要获取并重新设置
            // 由于无法获取原始过期时间，这里只是触发一次访问
            _memoryCache.TryGetValue(key, out _);
        }

        /// <summary>
        /// 重置值 (异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task RefreshAsync(string key)
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 移除
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        /// <summary>
        /// 移除 (异步)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task RemoveAsync(string key)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 存储值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value">值</param>
        /// <param name="exprireTime">过期时间（秒） 默认24小时</param>
        public void Set<T>(string key, T value, int exprireTime = 24 * 60 * 60)
        {
            var expiration = TimeSpan.FromSeconds(exprireTime);
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiration)
                .RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
                {
                    _keys.TryRemove(evictedKey.ToString(), out _);
                });

            _memoryCache.Set(key, value, options);
            _keys[key] = DateTime.UtcNow.Add(expiration);
        }

        /// <summary>
        /// 存储值 (异步)
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="exprireTime">过期时间（秒） 默认24小时</param>
        /// <returns></returns>
        public Task SetAsync<T>(string key, T value, int exprireTime = 24 * 60 * 60)
        {
            Set(key, value, exprireTime);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 清理过期的key记录
        /// </summary>
        private void CleanExpiredKeys()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _keys.Where(kv => kv.Value < now).Select(kv => kv.Key).ToList();
            foreach (var key in expiredKeys)
            {
                _keys.TryRemove(key, out _);
            }
        }
    }
}
