#nullable enable
using System.Collections.Generic;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 管理面板 IP 黑白名单配置。绑定 <see cref="SectionName"/>。
    /// 顺序：Deny 优先 → 若 <see cref="Allow"/> 非空则必须命中。
    /// 条目支持单 IP（<c>1.2.3.4</c> / <c>::1</c>）或 CIDR（<c>10.0.0.0/8</c> / <c>2001:db8::/32</c>）。
    /// </summary>
    public sealed class GatewayAdminIpFilterOptions
    {
        public const string SectionName = "Gateway:Admin:IpFilter";

        /// <summary>白名单。为空表示不启用白名单（不限制来源）。命中放行。</summary>
        public List<string> Allow { get; set; } = new();

        /// <summary>黑名单。命中即拒绝（403），优先级高于白名单。</summary>
        public List<string> Deny { get; set; } = new();

        /// <summary>是否信任 <see cref="ForwardedHeaderName"/> 头取真实客户端 IP。</summary>
        public bool TrustForwardedFor { get; set; }

        /// <summary>承载真实客户端 IP 的请求头名（默认 <c>X-Forwarded-For</c>）。</summary>
        public string ForwardedHeaderName { get; set; } = "X-Forwarded-For";
    }
}
