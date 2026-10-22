using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.Domain.Repository;
using SyZero.EventBus.DBEventBus.Entity;

namespace SyZero.EventBus.DBEventBus.Repository
{/// <summary>
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
                // 检查是否已存在锁
                var existingLock = await GetLeaderLockAsync(leaderKey);

                if (existingLock != null)
                {
                    // 如果锁未过期，返回 false
                    if (existingLock.ExpireTime > DateTime.UtcNow)
                    {
                        return false;
                    }

                    // 锁已过期，更新锁信息
                    existingLock.InstanceId = instanceId;
                    existingLock.AcquireTime = DateTime.UtcNow;
                    existingLock.RenewTime = DateTime.UtcNow;
                    existingLock.ExpireTime = DateTime.UtcNow.AddSeconds(expireSeconds);
                    await _repository.UpdateAsync(existingLock);
                }
                else
                {
                    // 创建新锁
                    var newLock = new LeaderLockEntity
                    {
                        LeaderKey = leaderKey,
                        InstanceId = instanceId,
                        AcquireTime = DateTime.UtcNow,
                        RenewTime = DateTime.UtcNow,
                        ExpireTime = DateTime.UtcNow.AddSeconds(expireSeconds)
                    };
                    await _repository.AddAsync(newLock);
                }

                return true;
            }
            catch
            {
                // 并发情况下可能失败
                return false;
            }
        }

        public async Task RenewLeaderLockAsync(string leaderKey, string instanceId, int expireSeconds)
        {
            var lockEntity = await GetLeaderLockAsync(leaderKey);
            if (lockEntity != null && lockEntity.InstanceId == instanceId)
            {
                lockEntity.RenewTime = DateTime.UtcNow;
                lockEntity.ExpireTime = DateTime.UtcNow.AddSeconds(expireSeconds);
                await _repository.UpdateAsync(lockEntity);
            }
        }

        public async Task ReleaseLeaderLockAsync(string leaderKey, string instanceId)
        {
            var lockEntity = await GetLeaderLockAsync(leaderKey);
            if (lockEntity != null && lockEntity.InstanceId == instanceId)
            {
                await _repository.DeleteAsync(lockEntity.Id);
            }
        }
    }
}
