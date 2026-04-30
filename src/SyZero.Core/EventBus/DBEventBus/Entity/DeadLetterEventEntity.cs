using System;
using System.Collections.Generic;
using System.Text;
using SyZero.Domain.Entities;

namespace SyZero.EventBus.DBEventBus.Entity
{
    /// <summary>
    /// 死信队列实体
    /// </summary>
    public class DeadLetterEventEntity : IEntity
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
        /// 事件数据（JSON格式）
        /// </summary>
        public string EventData { get; set; }

        /// <summary>
        /// 事件类型全名
        /// </summary>
        public string EventTypeName { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 最后错误信息
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// 原始创建时间
        /// </summary>
        public DateTime OriginalCreateTime { get; set; }

        /// <summary>
        /// 加入死信队列时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
