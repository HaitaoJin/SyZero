#nullable enable
using System;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关配置备份元数据。供管理面板列表展示与还原使用。
    /// </summary>
    public sealed class GatewayConfigurationBackup
    {
        /// <summary>
        /// 备份标识，由具体存储实现定义（文件型实现为去除前后缀的时间戳，如 <c>20260430153210</c>）。
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 备份创建时间。
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 备份内容大小（字节）。未知时为 0。
        /// </summary>
        public long SizeBytes { get; set; }
    }
}
