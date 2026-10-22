namespace SyZero.MultiTenancy
{
    /// <summary>多租户模块配置。</summary>
    public class MultiTenancyOptions
    {
        /// <summary>是否启用多租户中间件。</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Header 解析使用的请求头名称。</summary>
        public string TenantHeaderName { get; set; } = "__tenant";

        /// <summary>Domain 解析使用的域名后缀（如 <c>.example.com</c>），子域作为租户名。null 关闭。</summary>
        public string? DomainSuffix { get; set; }

        /// <summary>Claim 解析使用的租户 Id Claim 类型。</summary>
        public string TenantIdClaimType { get; set; } = "tenantid";

        /// <summary>Claim 解析使用的租户名 Claim 类型。</summary>
        public string TenantNameClaimType { get; set; } = "tenant";
    }
}
