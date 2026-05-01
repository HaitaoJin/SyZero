#nullable enable
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关路由 DTO。字段命名对齐 ocelot.json，便于多后端共用。
    /// </summary>
    public sealed class GatewayRoute
    {
        public string UpstreamPathTemplate { get; set; } = string.Empty;

        public List<string> UpstreamHttpMethod { get; set; } = new List<string>();

        public bool? UseServiceDiscovery { get; set; }

        public string DownstreamPathTemplate { get; set; } = string.Empty;

        public string DownstreamScheme { get; set; } = "http";

        public string? ServiceName { get; set; }

        public GatewayLoadBalancerOptions? LoadBalancerOptions { get; set; }

        /// <summary>
        /// 单路由限流配置。<c>null</c> 表示该路由不参与限流（与 ocelot.json 中省略 RateLimitOptions 等价）。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GatewayRateLimitOptions? RateLimitOptions { get; set; }

        public string? SwaggerKey { get; set; }

        /// <summary>
        /// 路由是否被禁用（默认 false）。
        /// 启用 Admin 面板的「批量启停」时可标记，<see cref="IGateway"/> 下发实现侧应跳过 <c>true</c> 的路由。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Disabled { get; set; }

        /// <summary>
        /// 路由标签，用于 Admin UI 分组 / 过滤 / 批量操作。下发到具体后端时被忽略。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Tags { get; set; }

        /// <summary>
        /// 路由描述（支持 Markdown）。仅作为元数据保存在 <c>configuration.json</c> 中，下发到具体后端时被忽略。
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>透传未识别字段，避免后端特有配置写回丢失。</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }
}
