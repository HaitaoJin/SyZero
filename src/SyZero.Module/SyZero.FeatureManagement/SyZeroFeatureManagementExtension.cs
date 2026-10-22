using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FeatureManagement;

namespace SyZero.FeatureManagement;

public static class SyZeroFeatureManagementExtension
{
    /// <summary>
    /// 注册 Microsoft.FeatureManagement 与 SyZero 适配。
    /// 默认从 IConfiguration 的 "FeatureManagement" 节读取开关。
    /// </summary>
    public static IServiceCollection AddSyZeroFeatureManagement(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration != null)
        {
            services.AddFeatureManagement(configuration.GetSection("FeatureManagement"));
        }
        else
        {
            services.AddFeatureManagement();
        }
        services.TryAddScoped<ISyFeatureManager, DefaultSyFeatureManager>();
        return services;
    }
}
