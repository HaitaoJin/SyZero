using Nacos.Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.Configuration
{
    public static class NacosConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddNacos(this IConfigurationBuilder builder, IConfiguration configuration, string sectionName = "NacosConfig")
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            ArgumentNullException.ThrowIfNull(configuration);

            return builder.AddNacosV2Configuration(configuration.GetSection(sectionName));
        }

        public static IConfigurationBuilder AddNacos(this IConfigurationBuilder builder, Action<NacosV2ConfigurationSource> action)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            return builder.AddNacosV2Configuration(action);
        }

        public static IConfigurationBuilder AddNacosConfiguration(this IConfigurationBuilder builder, IConfiguration configuration, string sectionName = "NacosConfig")
        {
            return builder.AddNacos(configuration, sectionName);
        }

        public static IConfigurationBuilder AddNacosConfiguration(this IConfigurationBuilder builder, Action<NacosV2ConfigurationSource> action)
        {
            return builder.AddNacos(action);
        }
    }
}
