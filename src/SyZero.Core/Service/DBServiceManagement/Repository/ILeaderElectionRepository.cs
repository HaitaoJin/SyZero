using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.Service.DBServiceManagement.Entity;

namespace SyZero.Service.DBServiceManagement.Repository
{
    /// <summary>
    /// Leader 选举仓储接口
    /// </summary>
    public interface ILeaderElectionRepository
    {
        /// <summary>
        /// 获取当前 Leader 锁信息
        /// </summary>
        /// <param name="leaderKey">锁的键</param>
        /// <returns>锁信息，如果不存在返回 null</returns>
        Task<LeaderLockEntity> GetLeaderLockAsync(string leaderKey);

        /// <summary>
        /// 尝试获取 Leader 锁
        /// </summary>
        /// <param name="leaderKey">锁的键</param>
        /// <param name="instanceId">实例ID</param>
        /// <param name="expireSeconds">过期时间（秒）</param>
        /// <returns>是否成功获取</returns>
        Task<bool> TryAcquireLeaderLockAsync(string leaderKey, string instanceId, int expireSeconds);

        /// <summary>
        /// 续期 Leader 锁
        /// </summary>
        /// <param name="leaderKey">锁的键</param>
        /// <param name="instanceId">实例ID</param>
        /// <param name="expireSeconds">过期时间（秒）</param>
        Task RenewLeaderLockAsync(string leaderKey, string instanceId, int expireSeconds);

        /// <summary>
        /// 释放 Leader 锁
        /// </summary>
        /// <param name="leaderKey">锁的键</param>
        /// <param name="instanceId">实例ID</param>
        Task ReleaseLeaderLockAsync(string leaderKey, string instanceId);
    }

}
