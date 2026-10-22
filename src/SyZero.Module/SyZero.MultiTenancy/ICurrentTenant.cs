using System;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 当前请求的租户上下文。Scoped 注入。
    /// </summary>
    public interface ICurrentTenant
    {
        /// <summary>当前租户 Id。null 表示宿主侧（未识别到租户）。</summary>
        long? Id { get; }

        /// <summary>当前租户名。</summary>
        string? Name { get; }

        /// <summary>当前租户完整元数据（如已解析）。</summary>
        TenantInfo? Tenant { get; }

        /// <summary>是否处于「可用租户」上下文（解析到且 IsActive）。</summary>
        bool IsAvailable { get; }

        /// <summary>显式切换上下文（如后台任务里手工进入某租户作用域）。返回 IDisposable，Dispose 时还原。</summary>
        IDisposable Change(TenantInfo? tenant);
    }
}
