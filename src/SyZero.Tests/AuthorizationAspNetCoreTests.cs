using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using SyZero.Authorization.AspNetCore;
using SyZero.Runtime.Security;
using Xunit;

namespace SyZero.Tests;

public class AuthorizationAspNetCoreTests
{
    private class FakePermissionChecker : IPermissionChecker
    {
        private readonly bool _allow;
        public FakePermissionChecker(bool allow) { _allow = allow; }
        public bool IsHas(string permission) => _allow;
        public void Authorize(string permission) { }
    }

    [Fact]
    public async Task AllowedPermission_HandlerSucceeds()
    {
        var sp = new ServiceCollection()
            .AddSingleton<IPermissionChecker>(new FakePermissionChecker(true))
            .AddSyZeroAuthorizationBridge()
            .BuildServiceProvider();

        var handler = (SyPermissionAuthorizationHandler)sp.GetServices<IAuthorizationHandler>()
            .First(h => h is SyPermissionAuthorizationHandler);

        var requirement = new SyPermissionRequirement("Order.Create");
        var ctx = new AuthorizationHandlerContext(new[] { requirement }, new System.Security.Claims.ClaimsPrincipal(), null);
        await handler.HandleAsync(ctx);
        Assert.True(ctx.HasSucceeded);
    }

    [Fact]
    public async Task DeniedPermission_HandlerDoesNotSucceed()
    {
        var sp = new ServiceCollection()
            .AddSingleton<IPermissionChecker>(new FakePermissionChecker(false))
            .AddSyZeroAuthorizationBridge()
            .BuildServiceProvider();

        var handler = (SyPermissionAuthorizationHandler)sp.GetServices<IAuthorizationHandler>()
            .First(h => h is SyPermissionAuthorizationHandler);

        var requirement = new SyPermissionRequirement("Order.Delete");
        var ctx = new AuthorizationHandlerContext(new[] { requirement }, new System.Security.Claims.ClaimsPrincipal(), null);
        await handler.HandleAsync(ctx);
        Assert.False(ctx.HasSucceeded);
    }

    [Fact]
    public void PolicyNameHelper_PrefixesCorrectly()
    {
        Assert.Equal("syperm:Order.Create", SyPermissionPolicy.For("Order.Create"));
    }
}
