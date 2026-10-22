using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.Util
{
    /// <summary>
    /// 基于内存的默认锁实现
    /// 注意：此实现仅适用于单机环境，分布式环境请使用 Redis 锁
    /// </summary>
    public class MemoryLockUtil : ILockUtil, ISingletonDependency
    {
        /// <summary>
        /// 锁存储字典，key 为锁名称，value 为过期时间
        /// </summary>
        private readonly ConcurrentDictionary<string, LockInfo> _locks;

        /// <summary>
        /// 默认过期时间 10 秒
        /// </summary>
        private readonly int _defaultExpires = 10;

        /// <summary>
        /// 等待时重试时间间隔：毫秒，默认 60 毫秒
        /// </summary>
        private readonly int _retryInterval = 60;

        /// <summary>
        /// 用于同步的锁对象
        /// </summary>
        private readonly object _syncLock = new object();

        public MemoryLockUtil()
        {
            _locks = new ConcurrentDictionary<string, LockInfo>();
        }

        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="lockKey">上锁的key</param>
        /// <param name="expiresSenconds">过期时间：秒，默认10秒</param>
        /// <param name="waitTimeSenconds">等待时间，默认10s</param>
        /// <returns></returns>
        public bool GetLock(string lockKey, int expiresSenconds = 10, int waitTimeSenconds = 10)
        {
            if (expiresSenconds <= 0)
            {
                expiresSenconds = _defaultExpires;
            }

            if (waitTimeSenconds <= 0)
            {
                return TryAcquireLock(lockKey, expiresSenconds);
            }

            var waitEndTime = DateTime.UtcNow.AddSeconds(waitTimeSenconds);
            bool result = false;

            while (!result && DateTime.UtcNow <= waitEndTime)
            {
                result = TryAcquireLock(lockKey, expiresSenconds);

                if (!result)
                {
                    Thread.Sleep(_retryInterval);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取锁（异步）
        /// </summary>
        /// <param name="lockKey">上锁的key</param>
        /// <param name="expiresSenconds">过期时间：秒，默认10秒</param>
        /// <param name="waitTimeSenconds">等待时间，默认10s</param>
        /// <returns></returns>
        public async Task<bool> GetLockAsync(string lockKey, int expiresSenconds = 10, int waitTimeSenconds = 10)
        {
            if (expiresSenconds <= 0)
            {
                expiresSenconds = _defaultExpires;
            }

            if (waitTimeSenconds <= 0)
            {
                return TryAcquireLock(lockKey, expiresSenconds);
            }

            var waitEndTime = DateTime.UtcNow.AddSeconds(waitTimeSenconds);
            bool result = false;

            while (!result && DateTime.UtcNow <= waitEndTime)
            {
                result = TryAcquireLock(lockKey, expiresSenconds);

                if (!result)
                {
                    var leftTime = (waitEndTime - DateTime.UtcNow).TotalMilliseconds;

                    if (leftTime >= _retryInterval)
                    {
                        await Task.Delay(_retryInterval);
                    }
                    else if (leftTime > 0)
                    {
                        await Task.Delay((int)leftTime);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockKey"></param>
        public void Release(string lockKey)
        {
            _locks.TryRemove(lockKey, out _);
        }

        /// <summary>
        /// 尝试获取锁
        /// </summary>
        /// <param name="lockKey">锁的key</param>
        /// <param name="expiresSenconds">过期时间（秒）</param>
        /// <returns></returns>
        private bool TryAcquireLock(string lockKey, int expiresSenconds)
        {
            lock (_syncLock)
            {
                // 清理过期的锁
                CleanExpiredLock(lockKey);

                // 检查锁是否存在
                if (_locks.ContainsKey(lockKey))
                {
                    return false;
                }

                // 添加锁
                var lockInfo = new LockInfo
                {
                    ExpireTime = DateTime.UtcNow.AddSeconds(expiresSenconds),
                    ThreadId = Thread.CurrentThread.ManagedThreadId
                };

                return _locks.TryAdd(lockKey, lockInfo);
            }
        }

        /// <summary>
        /// 清理过期的锁
        /// </summary>
        /// <param name="lockKey"></param>
        private void CleanExpiredLock(string lockKey)
        {
            if (_locks.TryGetValue(lockKey, out var lockInfo))
            {
                if (lockInfo.ExpireTime < DateTime.UtcNow)
                {
                    _locks.TryRemove(lockKey, out _);
                }
            }
        }

        /// <summary>
        /// 锁信息
        /// </summary>
        private class LockInfo
        {
            /// <summary>
            /// 过期时间
            /// </summary>
            public DateTime ExpireTime { get; set; }

            /// <summary>
            /// 持有锁的线程ID
            /// </summary>
            public int ThreadId { get; set; }
        }
    }
}
