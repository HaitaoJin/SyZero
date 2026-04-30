using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.EventBus.DBEventBus.Entity;

namespace SyZero.EventBus.DBEventBus.Repository
{

    /// <summary>
    /// Leader 选举仓储接口
    /// </summary>
    public interface ILeaderElectionRepository
    {
        Task<LeaderLockEntity> GetLeaderLockAsync(string leaderKey);
        Task<bool> TryAcquireLeaderLockAsync(string leaderKey, string instanceId, int expireSeconds);
        Task RenewLeaderLockAsync(string leaderKey, string instanceId, int expireSeconds);
        Task ReleaseLeaderLockAsync(string leaderKey, string instanceId);
    }
}
