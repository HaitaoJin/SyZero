using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SyZero.EventBus
{
    /// <summary>
    /// 事件总线接口
    /// </summary>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <typeparam name="TH">事件处理器类型</typeparam>
        /// <param name="handler">事件处理器工厂方法</param>
        void Subscribe<T, TH>(Func<TH> handler)
          where TH : IEventHandler<T>;

        /// <summary>
        /// 订阅事件（动态订阅）
        /// </summary>
        /// <typeparam name="TH">事件处理器类型</typeparam>
        /// <param name="eventName">事件名称</param>
        void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicEventHandler;

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <typeparam name="TH">事件处理器类型</typeparam>
        void Unsubscribe<T, TH>()
            where TH : IEventHandler<T>;

        /// <summary>
        /// 取消订阅事件（动态取消）
        /// </summary>
        /// <typeparam name="TH">事件处理器类型</typeparam>
        /// <param name="eventName">事件名称</param>
        void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicEventHandler;

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="event">集成事件</param>
        void Publish(EventBase @event);

        /// <summary>
        /// 异步发布事件
        /// </summary>
        /// <param name="event">集成事件</param>
        /// <returns></returns>
        Task PublishAsync(EventBase @event);

        /// <summary>
        /// 发布事件（指定事件名称）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        void Publish(string eventName, object eventData);

        /// <summary>
        /// 异步发布事件（指定事件名称）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="eventData">事件数据</param>
        /// <returns></returns>
        Task PublishAsync(string eventName, object eventData);

        /// <summary>
        /// 批量发布事件
        /// </summary>
        /// <param name="events">事件集合</param>
        void PublishBatch(IEnumerable<EventBase> events);

        /// <summary>
        /// 异步批量发布事件
        /// </summary>
        /// <param name="events">事件集合</param>
        /// <returns></returns>
        Task PublishBatchAsync(IEnumerable<EventBase> events);

        /// <summary>
        /// 清空所有订阅
        /// </summary>
        void Clear();

        /// <summary>
        /// 检查是否已订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns></returns>
        bool IsSubscribed<T>();

        /// <summary>
        /// 检查是否已订阅事件（通过事件名称）
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns></returns>
        bool IsSubscribed(string eventName);

        /// <summary>
        /// 获取所有订阅的事件名称
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetSubscribedEvents();
    }
}
