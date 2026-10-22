using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SyZero.Sms;

namespace SyZero;

/// <summary>SyZero.Sms 模块的 DI 扩展。</summary>
public static class SyZeroSmsExtension
{
    public static IServiceCollection AddSyZeroConsoleSms(this IServiceCollection services)
    {
        services.TryAddSingleton<ISySmsSender, ConsoleSmsSender>();
        return services;
    }
}
