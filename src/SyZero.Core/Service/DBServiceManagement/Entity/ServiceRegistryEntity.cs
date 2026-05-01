using System;
using System.Collections.Generic;
using System.Text;
using SyZero.Domain.Entities;

namespace SyZero.Service.DBServiceManagement.Entity
{
    /// <summary>
    /// 服务注册实体
    /// </summary>
    public class ServiceRegistryEntity : IEntity
    {
        public long Id { get; set; }
        public string ServiceID { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ServiceAddress { get; set; } = string.Empty;
        public int ServicePort { get; set; }
        public string ServiceProtocol { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public string Metadata { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public bool Enabled { get; set; }
        public double Weight { get; set; }
        public DateTime RegisterTime { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public string HealthCheckUrl { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
    }

}
