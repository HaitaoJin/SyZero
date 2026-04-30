using System;
using System.Collections.Generic;
using System.Text;
using SyZero.Domain.Entities;

namespace SyZero.EventBus.DBEventBus.Entity
{
    /// <summary>
    /// Leader 锁实体
    /// </summary>
    public class LeaderLockEntity : IEntity
    {
        public long Id { get; set; }
        public string LeaderKey { get; set; }
        public string InstanceId { get; set; }
        public DateTime AcquireTime { get; set; }
        public DateTime RenewTime { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}
