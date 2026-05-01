using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Realtime;

namespace SyZero;

/// <summary>
/// SyZero.Realtime DI / Endpoint 扩展。
/// </summary>
public static class SyZeroRealtimeExtension
{
    /// <summary>
    /// 注册 SignalR 与 SyZero 实时通知抽象。
    /// </summary>
    public static IServiceCollection AddSyZeroRealtime(this IServiceCollection services)
    {
        services.AddSignalR();
        return services;
    }

    /// <summary>
    /// 映射默认 Hub 到 <paramref name="pattern"/>（默认 /hubs/sy-realtime）。
    /// </summary>
    public static IEndpointRouteBuilder MapSyZeroRealtime(this IEndpointRouteBuilder endpoints, string pattern = "/hubs/sy-realtime")
    {
        endpoints.MapHub<SyRealtimeHub>(pattern);
        return endpoints;
    }
}
