using System;
using System.Collections.Generic;
using System.Text;
using SyZero.Runtime.Security;

namespace SyZero.Service
{
    public class ServiceInfo
    {
        #region 基础信息

        /// <summary>
        /// 服务ID
        /// </summary>
        public string ServiceID { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// 服务地址
        /// </summary>
        public string ServiceAddress { get; set; }

        /// <summary>
        /// 服务端口
        /// </summary>
        public int ServicePort { get; set; }

        /// <summary>
        /// 服务协议
        /// </summary>
        public ProtocolType ServiceProtocol { get; set; }

        /// <summary>
        /// 服务版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 服务分组/命名空间
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// 服务标签（用于分类和过滤）
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// 服务元数据（扩展信息）
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; }

        #endregion

        #region 健康状态

        /// <summary>
        /// 是否健康
        /// </summary>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        #endregion

        #region 负载均衡

        /// <summary>
        /// 权重（用于加权负载均衡）
        /// </summary>
        public double Weight { get; set; } = 1.0;

        #endregion

        #region 时间信息

        /// <summary>
        /// 注册时间
        /// </summary>
        public DateTime RegisterTime { get; set; }

        /// <summary>
        /// 最后心跳时间
        /// </summary>
        public DateTime? LastHeartbeat { get; set; }

        #endregion

        #region 网络相关

        /// <summary>
        /// 健康检查地址
        /// </summary>
        public string HealthCheckUrl { get; set; }

        /// <summary>
        /// 健康检查间隔（秒）
        /// </summary>
        public int HealthCheckIntervalSeconds { get; set; } = 10;

        /// <summary>
        /// 健康检查超时时间（秒）
        /// </summary>
        public int HealthCheckTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// 服务实例所在区域/数据中心
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// 可用区
        /// </summary>
        public string Zone { get; set; }

        #endregion
    }
}
