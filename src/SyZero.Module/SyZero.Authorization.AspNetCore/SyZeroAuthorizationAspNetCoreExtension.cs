using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SyZero.Authorization.AspNetCore;

public static class SyZeroAuthorizationAspNetCoreExtension
{
    /// <summary>
    /// 注册 SyZero 权限到 ASP.NET Core 授权管线的桥接。
    /// 调用前请确保已注册 IPermissionChecker（SyZero.Core 默认提供 DefaultPermissionChecker）。
    /// </summary>
    public static IServiceCollection AddSyZeroAuthorizationBridge(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, SyPermissionAuthorizationHandler>());
        return services;
    }
}
