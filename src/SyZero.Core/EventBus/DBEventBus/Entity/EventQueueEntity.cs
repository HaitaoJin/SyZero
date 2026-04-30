using System;
using System.Collections.Generic;
using System.Text;
using SyZero.Domain.Entities;

namespace SyZero.EventBus.DBEventBus.Entity
{

    /// <summary>
    /// 事件队列实体
    /// </summary>
    public class EventQueueEntity : IEntity
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
        /// 状态：0-待处理，1-处理中，2-已完成，3-失败
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 最后重试时间
        /// </summary>
        public DateTime? LastRetryTime { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 处理时间
        /// </summary>
        public DateTime? ProcessTime { get; set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompleteTime { get; set; }
    }

}
