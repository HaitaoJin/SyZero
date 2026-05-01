#nullable enable
using System;

namespace SyZero.Gateway.Admin.Events
{
    /// <summary>
    /// 网关管理面板对外广播的事件（写操作之后）。
    /// </summary>
    public sealed class GatewayAdminEvent
    {
        /// <summary>事件类型，与审计 <c>Action</c> 字段一致，例如 <c>route.update</c> / <c>config.reload</c>。</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>触发用户。</summary>
        public string? User { get; set; }

        /// <summary>触发目标，如路由下标、Swagger Key、备份 Id。</summary>
        public string? Target { get; set; }

        /// <summary>摘要（人类可读）。</summary>
        public string? Summary { get; set; }

        /// <summary>UTC 时间戳。</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
