using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.Domain.Repository;
using SyZero.Service.DBServiceManagement.Entity;

namespace SyZero.Service.DBServiceManagement.Repository
{

    /// <summary>
    /// Leader 选举仓储默认实现
    /// </summary>
    public class LeaderElectionRepository : ILeaderElectionRepository
    {
        private readonly IRepository<LeaderLockEntity> _repository;

        public LeaderElectionRepository(IRepository<LeaderLockEntity> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<LeaderLockEntity> GetLeaderLockAsync(string leaderKey)
        {
            return await _repository.GetModelAsync(x => x.LeaderKey == leaderKey);
        }

        public async Task<bool> TryAcquireLeaderLockAsync(string leaderKey, string instanceId, int expireSeconds)
        {
            try
            {
                var existingLock = await _repository.GetModelAsync(x => x.LeaderKey == leaderKey);
                var now = DateTime.UtcNow;
                var expireTime = now.AddSeconds(expireSeconds);

                if (existingLock == null)
                {
                    // 不存在锁，直接创建
                    var newLock = new LeaderLockEntity
                    {
                        LeaderKey = leaderKey,
                        InstanceId = instanceId,
                        AcquireTime = now,
                        RenewTime = now,
                        ExpireTime = expireTime
                    };
                    await _repository.AddAsync(newLock);
                    return true;
                }

                // 锁已过期，可以抢占
                if (existingLock.ExpireTime <= now)
                {
                    existingLock.InstanceId = instanceId;
                    existingLock.AcquireTime = now;
                    existingLock.RenewTime = now;
                    existingLock.ExpireTime = expireTime;
                    await _repository.UpdateAsync(existingLock);
                    return true;
                }

                // 锁未过期
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task RenewLeaderLockAsync(string leaderKey, string instanceId, int expireSeconds)
        {
            var existingLock = await _repository.GetModelAsync(x => x.LeaderKey == leaderKey && x.InstanceId == instanceId);
            if (existingLock != null)
            {
                var now = DateTime.UtcNow;
                existingLock.RenewTime = now;
                existingLock.ExpireTime = now.AddSeconds(expireSeconds);
                await _repository.UpdateAsync(existingLock);
            }
        }

        public async Task ReleaseLeaderLockAsync(string leaderKey, string instanceId)
        {
            var existingLock = await _repository.GetModelAsync(x => x.LeaderKey == leaderKey && x.InstanceId == instanceId);
            if (existingLock != null)
            {
                await _repository.DeleteAsync(existingLock.Id);
            }
        }
    }
}
