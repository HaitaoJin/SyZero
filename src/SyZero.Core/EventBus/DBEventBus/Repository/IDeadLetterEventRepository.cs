using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.EventBus.DBEventBus.Entity;

namespace SyZero.EventBus.DBEventBus.Repository
{
    /// <summary>
    /// 死信队列仓储接口
    /// </summary>
    public interface IDeadLetterEventRepository
    {
        Task<List<DeadLetterEventEntity>> GetAllAsync();
        Task<DeadLetterEventEntity> GetByIdAsync(long id);
        Task AddAsync(DeadLetterEventEntity entity);
        Task DeleteAsync(long id);
    }
}
