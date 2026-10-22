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
    /// 事件订阅仓储默认实现
    /// </summary>
    public class EventSubscriptionRepository : IEventSubscriptionRepository
    {
        private readonly IRepository<EventSubscriptionEntity> _repository;

        public EventSubscriptionRepository(IRepository<EventSubscriptionEntity> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<EventSubscriptionEntity>> GetByEventNameAsync(string eventName)
        {
            var query = await _repository.GetListAsync(x => x.EventName == eventName);
            return query.ToList();
        }

        public async Task<EventSubscriptionEntity> GetByEventAndHandlerAsync(string eventName, string handlerTypeName)
        {
            return await _repository.GetModelAsync(x =>
                x.EventName == eventName &&
                x.HandlerTypeName == handlerTypeName);
        }

        public async Task<bool> HasSubscriptionsAsync(string eventName)
        {
            var count = await _repository.CountAsync(x => x.EventName == eventName);
            return count > 0;
        }

        public async Task<List<string>> GetAllEventNamesAsync()
        {
            var query = await _repository.GetListAsync();
            return query.Select(x => x.EventName).Distinct().ToList();
        }

        public async Task AddAsync(EventSubscriptionEntity entity)
        {
            await _repository.AddAsync(entity);
        }

        public async Task RemoveAsync(string eventName, string handlerTypeName)
        {
            await _repository.DeleteAsync(x =>
                x.EventName == eventName &&
                x.HandlerTypeName == handlerTypeName);
        }

        public async Task ClearAsync()
        {
            var all = await _repository.GetListAsync();
            if (all.Any())
            {
                await _repository.DeleteAsync(x => true);
            }
        }
    }

}
