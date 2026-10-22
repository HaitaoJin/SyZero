using Microsoft.Extensions.Configuration;
using SyZero.Gateway;

namespace SyZero.Gateway.YARP
{
    /// <summary>
    /// SyZero.Gateway.YARP 模块的选项。继承 <see cref="GatewayOptions"/> 复用通用 CORS / Swagger 开关，
    /// 并追加 YARP 专属配置（配置文件名、是否启用从 appsettings 加载等）。
    /// </summary>
    public sealed class SyZeroYarpOptions : GatewayOptions
    {
        public SyZeroYarpOptions()
        {
            CorsPolicyName = "SyZero.Gateway.YARP";
            SwaggerTitle = "SyZero YARP Gateway";
            EnableSwagger = false;
            EnableSwaggerGen = false;
        }

        /// <summary>
        /// 是否在启动时从 <c>configuration.json</c>（与 Ocelot 共享相同 schema）加载初始路由。
        /// 关闭后初始路由集为空，需要通过管理面板/Applier 注入。
        /// </summary>
        public bool LoadFromConfigurationFile { get; set; } = true;

        /// <summary>
        /// 配置文件相对内容根目录的路径。默认 <c>configuration.json</c>，与 Ocelot 一致以便互换。
        /// </summary>
        public string ConfigurationFileName { get; set; } = "configuration.json";

        /// <summary>
        /// 是否额外从 <see cref="IConfiguration"/> 的 <c>ReverseProxy</c> 段载入 YARP 原生配置；启用后
        /// 与文件型 SyZero 路由会合并到同一个内存提供者。
        /// </summary>
        public bool LoadFromReverseProxySection { get; set; } = false;

        public static new SyZeroYarpOptions CreateDefault(IConfiguration configuration)
        {
            var options = new SyZeroYarpOptions();
            ApplyDefaults(options, configuration);
            return options;
        }
    }
}
