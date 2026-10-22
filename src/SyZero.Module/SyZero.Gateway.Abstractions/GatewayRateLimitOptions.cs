#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关「单路由」限流选项。字段对齐 Ocelot 的 <c>RateLimitOptions</c> 习惯，便于多后端共用。
    /// 所有字段均可空：null 表示不变更。<see cref="EnableRateLimiting"/> 为 false 时其它字段被实现侧忽略。
    /// </summary>
    public sealed class GatewayRateLimitOptions
    {
        /// <summary>是否启用限流。默认 false。</summary>
        public bool EnableRateLimiting { get; set; }

        /// <summary>窗口长度，格式 <c>1s/5m/1h/1d</c>。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Period { get; set; }

        /// <summary>窗口超时（秒）。Ocelot 中是「客户端达到 Limit 后等待多久才能再次发起请求」。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public double? PeriodTimespan { get; set; }

        /// <summary>同窗口内允许的最大请求数。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Limit { get; set; }

        /// <summary>客户端白名单（按 ClientId 匹配）。</summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? ClientWhitelist { get; set; }
    }
}
