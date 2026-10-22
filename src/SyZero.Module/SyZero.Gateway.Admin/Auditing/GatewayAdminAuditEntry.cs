#nullable enable
using System;

namespace SyZero.Gateway.Admin.Auditing
{
    /// <summary>
    /// 一条网关管理审计记录。
    /// </summary>
    public sealed class GatewayAdminAuditEntry
    {
        /// <summary>UTC 时间戳。</summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>操作用户名（来自 Basic Auth）。</summary>
        public string? User { get; set; }

        /// <summary>客户端 IP。</summary>
        public string? Ip { get; set; }

        /// <summary>操作动作，例如 <c>route.create</c> / <c>route.update</c> / <c>backup.restore</c>。</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>操作目标，例如路由下标、Swagger Key、备份 Id 等。</summary>
        public string? Target { get; set; }

        /// <summary>简要摘要（人类可读）。</summary>
        public string? Summary { get; set; }
    }
}
