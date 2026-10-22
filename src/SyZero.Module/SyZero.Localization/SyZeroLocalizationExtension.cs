using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SyZero.Localization;

public static class SyZeroLocalizationExtension
{
    /// <summary>
    /// 注册 .resx 资源本地化与 SyZero 适配。<paramref name="resourcesPath"/> 默认 "Resources"。
    /// </summary>
    public static IServiceCollection AddSyZeroLocalization(this IServiceCollection services, string resourcesPath = "Resources", Action<Microsoft.Extensions.Localization.LocalizationOptions>? configure = null)
    {
        services.AddLocalization(o =>
        {
            o.ResourcesPath = resourcesPath;
            configure?.Invoke(o);
        });
        services.TryAddScoped<ISyStringLocalizer, DefaultSyStringLocalizer>();
        return services;
    }
}
