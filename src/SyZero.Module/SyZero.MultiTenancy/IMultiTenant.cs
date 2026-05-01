using System;

namespace SyZero.MultiTenancy
{
    /// <summary>
    /// 多租户实体标记接口。EF Core / SqlSugar 全局过滤器以此识别需要按 TenantId 过滤的实体。
    /// </summary>
    public interface IMultiTenant
    {
        /// <summary>所属租户 Id（null 表示宿主侧 / 共享数据）。</summary>
        long? TenantId { get; set; }
    }
}
