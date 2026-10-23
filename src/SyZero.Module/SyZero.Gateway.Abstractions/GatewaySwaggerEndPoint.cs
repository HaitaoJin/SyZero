#nullable enable
using System.Collections.Generic;

namespace SyZero.Gateway
{
    /// <summary>
    /// Swagger 聚合端点。字段命名对齐 ocelot.json。
    /// </summary>
    public sealed class GatewaySwaggerEndPoint
    {
        public string Key { get; set; } = string.Empty;

        public List<GatewaySwaggerEndPointConfig> Config { get; set; } = new List<GatewaySwaggerEndPointConfig>();
    }

    public sealed class GatewaySwaggerEndPointConfig
    {
        public string Name { get; set; } = string.Empty;

        public string Version { get; set; } = "v1";

        public GatewaySwaggerService? Service { get; set; }

        public string? Url { get; set; }
    }

    public sealed class GatewaySwaggerService
    {
        public string Name { get; set; } = string.Empty;

        public string Path { get; set; } = "/openapi/v1.json";
    }
}
