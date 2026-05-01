using Microsoft.AspNetCore.Authorization;

namespace SyZero.Authorization.AspNetCore;

/// <summary>
/// 把 SyZero 权限作为 ASP.NET Core 授权策略来声明。
/// 用法：<c>[Authorize(Policy = SyPermissionPolicy.Prefix + "Order.Create")]</c>，
/// 或 <c>options.AddSyPermission("Order.Create")</c>。
/// </summary>
public static class SyPermissionPolicy
{
    /// <summary>策略名前缀。</summary>
    public const string Prefix = "syperm:";

    public static string For(string permission) => Prefix + permission;
}

public static class AuthorizationOptionsExtensions
{
    /// <summary>
    /// 注册一条以 SyZero 权限为要求的策略，策略名为 <see cref="SyPermissionPolicy.For"/>(permission)。
    /// </summary>
    public static AuthorizationOptions AddSyPermission(this AuthorizationOptions options, string permission)
    {
        options.AddPolicy(SyPermissionPolicy.For(permission), policy => policy.Requirements.Add(new SyPermissionRequirement(permission)));
        return options;
    }
}
