using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SyZero.Runtime.Security;

namespace SyZero.Authorization.AspNetCore;

/// <summary>
/// 表示「需要某个 SyZero 权限」的策略要求。
/// </summary>
public class SyPermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public SyPermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

/// <summary>
/// 把 ASP.NET Core 授权策略桥接到 <see cref="IPermissionChecker"/>。
/// </summary>
public class SyPermissionAuthorizationHandler : AuthorizationHandler<SyPermissionRequirement>
{
    private readonly IPermissionChecker _checker;

    public SyPermissionAuthorizationHandler(IPermissionChecker checker)
    {
        _checker = checker;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SyPermissionRequirement requirement)
    {
        if (_checker.IsHas(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
