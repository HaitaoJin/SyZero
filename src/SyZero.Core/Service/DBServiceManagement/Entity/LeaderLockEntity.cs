using System;
using System.Collections.Generic;
using System.Text;
using SyZero.Domain.Entities;

namespace SyZero.Service.DBServiceManagement.Entity
{
    /// <summary>
    /// Leader 锁实体
    /// </summary>
    public class LeaderLockEntity : IEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 锁的键
        /// </summary>
        public string LeaderKey { get; set; }

        /// <summary>
        /// 持有锁的实例ID
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// 获取锁的时间
        /// </summary>
        public DateTime AcquireTime { get; set; }

        /// <summary>
        /// 最后续期时间
        /// </summary>
        public DateTime RenewTime { get; set; }

        /// <summary>
        /// 锁过期时间
        /// </summary>
        public DateTime ExpireTime { get; set; }
    }

}
