using System;

namespace SyZero.EventBus
{
    /// <summary>
    /// 集成事件基类
    /// </summary>
    public class EventBase
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public EventBase()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }

        /// <summary>
        /// 事件ID
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationDate { get; private set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public virtual string EventName => GetType().Name;
    }
}
