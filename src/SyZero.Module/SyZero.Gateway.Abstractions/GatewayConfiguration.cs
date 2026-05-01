#nullable enable
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关顶层配置。字段命名沿用 ocelot.json 习惯（PascalCase），便于与现有 configuration.json 兼容；
    /// 其它后端实现负责自身的 mapping。
    /// </summary>
    public sealed class GatewayConfiguration
    {
        /// <summary>路由列表。</summary>
        public List<GatewayRoute> Routes { get; set; } = new List<GatewayRoute>();

        /// <summary>Swagger 聚合端点列表。</summary>
        public List<GatewaySwaggerEndPoint> SwaggerEndPoints { get; set; } = new List<GatewaySwaggerEndPoint>();

        /// <summary>透传未识别的根级字段（如 GlobalConfiguration），保证写回时不丢失。</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

        // ---------- 全局限流读写桥（GlobalConfiguration.RateLimitOptions） ----------

        private const string GlobalKey = "GlobalConfiguration";
        private const string RateLimitKey = "RateLimitOptions";

        /// <summary>读取 <c>GlobalConfiguration.RateLimitOptions</c>。不存在时返回 null。</summary>
        public GatewayGlobalRateLimitOptions? GetGlobalRateLimit()
        {
            if (Extra is null) return null;
            if (!Extra.TryGetValue(GlobalKey, out var globalElement)) return null;
            if (globalElement.ValueKind != JsonValueKind.Object) return null;
            if (!globalElement.TryGetProperty(RateLimitKey, out var rl)) return null;
            try
            {
                return JsonSerializer.Deserialize<GatewayGlobalRateLimitOptions>(rl.GetRawText());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 写入 / 覆盖 <c>GlobalConfiguration.RateLimitOptions</c>。
        /// 传入 <c>null</c> 会移除该子节点（保留 GlobalConfiguration 其它字段）。
        /// </summary>
        public void SetGlobalRateLimit(GatewayGlobalRateLimitOptions? value)
        {
            Extra ??= new Dictionary<string, JsonElement>();
            JsonObject globalObj;
            if (Extra.TryGetValue(GlobalKey, out var existing) && existing.ValueKind == JsonValueKind.Object)
            {
                var node = JsonNode.Parse(existing.GetRawText());
                globalObj = node?.AsObject() ?? new JsonObject();
            }
            else
            {
                globalObj = new JsonObject();
            }

            if (value is null)
            {
                globalObj.Remove(RateLimitKey);
            }
            else
            {
                var rlJson = JsonSerializer.SerializeToNode(value);
                if (rlJson is null)
                {
                    globalObj.Remove(RateLimitKey);
                }
                else
                {
                    globalObj[RateLimitKey] = rlJson;
                }
            }

            // 重新序列化回 JsonElement（Extra 的元素类型）。
            using var doc = JsonDocument.Parse(globalObj.ToJsonString());
            Extra[GlobalKey] = doc.RootElement.Clone();
        }
    }
}

