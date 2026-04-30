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
        public string ServiceID { get; set; }
        public string ServiceName { get; set; }
        public string ServiceAddress { get; set; }
        public int ServicePort { get; set; }
        public string ServiceProtocol { get; set; }
        public string Version { get; set; }
        public string Group { get; set; }
        public string Tags { get; set; }
        public string Metadata { get; set; }
        public bool IsHealthy { get; set; }
        public bool Enabled { get; set; }
        public double Weight { get; set; }
        public DateTime RegisterTime { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public string HealthCheckUrl { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }
    }

}
