#nullable enable
using System.Text.Json.Serialization;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关「全局」限流配置。对应 ocelot.json 中的
    /// <c>GlobalConfiguration.RateLimitOptions</c> 节点。
    /// 所有字段均可空，<c>null</c> 表示沿用后端默认。
    /// </summary>
    public sealed class GatewayGlobalRateLimitOptions
    {
        /// <summary>是否在响应头中关闭限流计数（X-Rate-Limit-*）。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? DisableRateLimitHeaders { get; set; }

        /// <summary>触发限流后返回的提示文案。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? QuotaExceededMessage { get; set; }

        /// <summary>触发限流时返回的 HTTP 状态码（默认 429）。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? HttpStatusCode { get; set; }

        /// <summary>用于识别客户端的请求头名称（Ocelot 默认 ClientId）。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ClientIdHeader { get; set; }
    }
}
