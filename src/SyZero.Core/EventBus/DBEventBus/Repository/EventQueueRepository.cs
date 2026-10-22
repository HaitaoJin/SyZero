using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyZero.Domain.Repository;
using SyZero.EventBus.DBEventBus.Entity;

namespace SyZero.EventBus.DBEventBus.Repository
{
    /// <summary>
    /// 事件队列仓储默认实现
    /// </summary>
    public class EventQueueRepository : IEventQueueRepository
    {
        private readonly IRepository<EventQueueEntity> _repository;

        public EventQueueRepository(IRepository<EventQueueEntity> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<EventQueueEntity>> GetPendingEventsAsync(int maxCount)
        {
            var query = await _repository.GetPagedAsync(
                1,
                maxCount,
                x => x.CreateTime,
                x => x.Status == 0,
                false);
            return query.ToList();
        }

        public async Task<EventQueueEntity> GetByIdAsync(long id)
        {
            return await _repository.GetModelAsync(id);
        }

        public async Task AddAsync(EventQueueEntity entity)
        {
            await _repository.AddAsync(entity);
        }

        public async Task UpdateAsync(EventQueueEntity entity)
        {
            await _repository.UpdateAsync(entity);
        }

        public async Task DeleteAsync(long id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task CleanExpiredEventsAsync(int expireSeconds)
        {
            var expireTime = DateTime.UtcNow.AddSeconds(-expireSeconds);
            await _repository.DeleteAsync(x =>
                x.Status == 2 && // 已完成状态
                x.CompleteTime.HasValue &&
                x.CompleteTime.Value < expireTime);
        }
    }
}
