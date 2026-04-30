using System;
using System.Collections.Generic;
using System.Text;
using SyZero.Domain.Entities;

namespace SyZero.EventBus.DBEventBus.Entity
{
    /// <summary>
    /// 事件订阅实体
    /// </summary>
    public class EventSubscriptionEntity : IEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 事件名称
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 处理器类型全名
        /// </summary>
        public string HandlerTypeName { get; set; }

        /// <summary>
        /// 是否为动态订阅
        /// </summary>
        public bool IsDynamic { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }

}
