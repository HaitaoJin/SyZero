using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SyZero.EventBus.DBEventBus.Entity;

namespace SyZero.EventBus.DBEventBus.Repository
{
    /// <summary>
    /// 事件订阅仓储接口
    /// </summary>
    public interface IEventSubscriptionRepository
    {
        Task<List<EventSubscriptionEntity>> GetByEventNameAsync(string eventName);
        Task<EventSubscriptionEntity> GetByEventAndHandlerAsync(string eventName, string handlerTypeName);
        Task<bool> HasSubscriptionsAsync(string eventName);
        Task<List<string>> GetAllEventNamesAsync();
        Task AddAsync(EventSubscriptionEntity entity);
        Task RemoveAsync(string eventName, string handlerTypeName);
        Task ClearAsync();
    }
}
