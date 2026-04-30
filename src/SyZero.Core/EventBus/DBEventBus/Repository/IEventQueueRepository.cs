using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.EventBus.DBEventBus.Entity;

namespace SyZero.EventBus.DBEventBus.Repository
{
    /// <summary>
    /// 事件队列仓储接口
    /// </summary>
    public interface IEventQueueRepository
    {
        Task<List<EventQueueEntity>> GetPendingEventsAsync(int maxCount);
        Task<EventQueueEntity> GetByIdAsync(long id);
        Task AddAsync(EventQueueEntity entity);
        Task UpdateAsync(EventQueueEntity entity);
        Task DeleteAsync(long id);
        Task CleanExpiredEventsAsync(int expireSeconds);
    }
}
