#nullable enable
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace SyZero.Gateway
{
    /// <summary>
    /// 网关公共选项基类。具体网关后端可继承本类追加专属开关（参见 SyZero.Ocelot 的 SyZeroOcelotOptions）。
    /// </summary>
    public class GatewayOptions
    {
        public const string DefaultCorsPolicyName = "SyZero.Gateway";

        public string CorsPolicyName { get; set; } = DefaultCorsPolicyName;

        public bool EnableSwagger { get; set; } = true;

        public bool EnableSwaggerGen { get; set; } = true;

        public string SwaggerDocumentName { get; set; } = "v1";

        public string SwaggerTitle { get; set; } = "SyZero Gateway";

        public string SwaggerGeneratorPath { get; set; } = "/swagger/docs";

        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 基于配置创建默认选项。会读取 <c>SyZero:Name</c> 作为 Swagger 标题、<c>SyZero:CorsOrigins</c> 作为允许源。
        /// </summary>
        public static GatewayOptions CreateDefault(IConfiguration configuration)
        {
            var options = new GatewayOptions();
            ApplyDefaults(options, configuration);
            return options;
        }

        /// <summary>
        /// 把 <c>SyZero:Name</c> 与 <c>SyZero:CorsOrigins</c> 应用到既有选项。供子类复用。
        /// </summary>
        protected static void ApplyDefaults(GatewayOptions options, IConfiguration configuration)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));
            if (configuration is null) return;

            var serverName = configuration["SyZero:Name"];
            if (!string.IsNullOrWhiteSpace(serverName))
            {
                options.SwaggerTitle = serverName;
            }
            options.AllowedOrigins = ResolveAllowedOrigins(configuration);
        }

        public virtual void Validate()
        {
            if (string.IsNullOrWhiteSpace(CorsPolicyName))
            {
                throw new ArgumentException("CorsPolicyName cannot be empty.", nameof(CorsPolicyName));
            }
            if (string.IsNullOrWhiteSpace(SwaggerDocumentName))
            {
                throw new ArgumentException("SwaggerDocumentName cannot be empty.", nameof(SwaggerDocumentName));
            }
            if (string.IsNullOrWhiteSpace(SwaggerTitle))
            {
                throw new ArgumentException("SwaggerTitle cannot be empty.", nameof(SwaggerTitle));
            }
            if (string.IsNullOrWhiteSpace(SwaggerGeneratorPath))
            {
                throw new ArgumentException("SwaggerGeneratorPath cannot be empty.", nameof(SwaggerGeneratorPath));
            }
        }

        private static string[] ResolveAllowedOrigins(IConfiguration configuration)
        {
            var origins = configuration["SyZero:CorsOrigins"];
            if (string.IsNullOrWhiteSpace(origins))
            {
                return Array.Empty<string>();
            }

            return origins
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
