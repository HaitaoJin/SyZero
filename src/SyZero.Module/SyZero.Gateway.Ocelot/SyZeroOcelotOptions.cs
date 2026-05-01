using Microsoft.Extensions.Configuration;
using SyZero.Gateway;

namespace SyZero.Gateway.Ocelot
{
    /// <summary>
    /// SyZero.Gateway.Ocelot 模块的选项。包含 <see cref="GatewayOptions"/> 中的通用配置，
    /// 以及 Ocelot 专属的 Consul / Polly / CacheManager 等开关。
    /// </summary>
    public sealed class SyZeroOcelotOptions : GatewayOptions
    {
        public SyZeroOcelotOptions()
        {
            CorsPolicyName = "SyZero.Gateway.Ocelot";
            SwaggerTitle = "SyZero API Gateway";
        }

        public bool EnableConsul { get; set; } = true;

        public bool UseConsulServiceAddress { get; set; } = true;

        public bool EnableConfigStoredInConsul { get; set; } = true;

        public bool EnableCacheManager { get; set; } = true;

        public bool EnablePolly { get; set; } = true;

        /// <summary>
        /// 是否启用基于 <see cref="Service.IServiceManagement"/> 的统一服务发现 Provider；
        /// 启用后可在 <c>ocelot.json</c> 通过 <c>"Type": "SyZero"</c> 指定 Ocelot 走该 Provider，
        /// 从而把任意 SyZero 服务发现后端（Consul / Nacos / Redis / Local / DB）接入 Ocelot。
        /// </summary>
        public bool EnableSyZeroServiceDiscovery { get; set; } = false;

        /// <summary>
        /// 基于配置创建默认的 Ocelot 选项。
        /// </summary>
        public static new SyZeroOcelotOptions CreateDefault(IConfiguration configuration)
        {
            var options = new SyZeroOcelotOptions();
            ApplyDefaults(options, configuration);
            return options;
        }
    }
}
