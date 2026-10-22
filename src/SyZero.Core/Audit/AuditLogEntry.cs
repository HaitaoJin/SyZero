using System;
using System.Collections.Generic;

namespace SyZero
{
    /// <summary>
    /// 审计日志条目。
    /// </summary>
    public class AuditLogEntry
    {
        /// <summary>
        /// 全局唯一 ID。
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 操作发生的时间（UTC）。
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 操作的用户 ID（可为空，表示匿名）。
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// 用户名（冗余字段，便于查询）。
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// 租户 ID（可为空）。
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// 操作名称，例如方法全名或业务动作标识。
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// 目标资源类型，例如实体类型名或 API 路径。
        /// </summary>
        public string? Resource { get; set; }

        /// <summary>
        /// 目标资源 ID。
        /// </summary>
        public string? ResourceId { get; set; }

        /// <summary>
        /// 客户端 IP。
        /// </summary>
        public string? ClientIp { get; set; }

        /// <summary>
        /// 操作耗时（毫秒）。
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// 是否成功（false 时通常配合 <see cref="ExceptionMessage"/>）。
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 异常信息（失败时）。
        /// </summary>
        public string? ExceptionMessage { get; set; }

        /// <summary>
        /// 附加上下文键值（已脱敏）。
        /// </summary>
        public Dictionary<string, string?> Extras { get; set; } = new Dictionary<string, string?>();
    }
}
