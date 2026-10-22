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
    /// 死信队列仓储默认实现
    /// </summary>
    public class DeadLetterEventRepository : IDeadLetterEventRepository
    {
        private readonly IRepository<DeadLetterEventEntity> _repository;

        public DeadLetterEventRepository(IRepository<DeadLetterEventEntity> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<DeadLetterEventEntity>> GetAllAsync()
        {
            var query = await _repository.GetListAsync();
            return query.ToList();
        }

        public async Task<DeadLetterEventEntity> GetByIdAsync(long id)
        {
            return await _repository.GetModelAsync(id);
        }

        public async Task AddAsync(DeadLetterEventEntity entity)
        {
            await _repository.AddAsync(entity);
        }

        public async Task DeleteAsync(long id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
