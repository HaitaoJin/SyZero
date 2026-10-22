using System;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 租户元数据。
    /// </summary>
    public class TenantInfo
    {
        public long Id { get; set; }

        /// <summary>租户唯一名称（用于 Header / Domain 解析键）。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>显示名。</summary>
        public string? DisplayName { get; set; }

        /// <summary>是否启用。禁用的租户解析后会被视为「未识别」。</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>可选连接字符串。配合 <see cref="ITenantConnectionResolver"/> 实现分库。</summary>
        public string? ConnectionString { get; set; }
    }
}
