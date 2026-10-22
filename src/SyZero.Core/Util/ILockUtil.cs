using System.Threading.Tasks;

namespace SyZero.Util
{
    public interface ILockUtil
    {
        /// <summary>
        /// 释放锁
        /// </summary>
        /// <param name="lockKey"></param>
        void Release(string lockKey);
        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="key">上锁的key</param>
        /// <param name="expiresMS">过期时间：秒，默认10秒，传入的expiresSenconds如果小于等于0，为了防止死锁：那么expiresSenconds也会调整为10</param>
        /// <param name="waitTimeSenconds">等待时间，默认10s</param>
        /// <returns></returns>
        bool GetLock(string lockKey, int expiresSenconds = 10, int waitTimeSenconds = 10);
        /// <summary>
        /// 获取锁
        /// </summary>
        /// <param name="key">上锁的key</param>
        /// <param name="expiresMS">过期时间：秒，默认10秒。传入的expiresSenconds如果小于等于0，为了防止死锁：那么expiresSenconds也会调整为10</param>
        /// <param name="waitTimeSenconds">等待时间，默认10s</param>
        /// <returns></returns>
        Task<bool> GetLockAsync(string lockKey, int expiresSenconds = 10, int waitTimeSenconds = 10);
    }
}
