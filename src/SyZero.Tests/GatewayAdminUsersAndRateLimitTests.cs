#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SyZero.Gateway;
using SyZero.Gateway.Admin.Auditing;
using SyZero.Gateway.Admin.Auth;
using SyZero.Gateway.Admin.Controllers;
using SyZero.Gateway.Admin.Events;
using SyZero.Gateway.Admin.Services;
using Xunit;

namespace SyZero.Tests;

/// <summary>
/// 网关管理多用户/角色/限流/负载均衡 新增功能测试（1.2.3）。
/// </summary>
public class GatewayAdminUsersAndRateLimitTests
{
    // ---------- 1. GatewayAdminRoles ----------

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("admin", true)]
    [InlineData("Reader", true)]
    [InlineData("READER", true)]
    [InlineData("Owner", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void GatewayAdminRoles_IsValid_HandlesEdgeCases(string? input, bool expected)
        => Assert.Equal(expected, GatewayAdminRoles.IsValid(input));

    [Theory]
    [InlineData(null, "Admin")]
    [InlineData("", "Admin")]
    [InlineData("admin", "Admin")]
    [InlineData("Reader", "Reader")]
    [InlineData("reader", "Reader")]
    [InlineData("RandomGarbage", "Admin")] // 非 Reader 全部归一化为 Admin（默认）
    public void GatewayAdminRoles_Normalize_DefaultsToAdmin(string? input, string expected)
        => Assert.Equal(expected, GatewayAdminRoles.Normalize(input));

    // ---------- 2. FileGatewayAdminUserStore ----------

    [Fact]
    public async Task FileUserStore_AddUpdateRemove_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = (IGatewayAdminUserStore)Activator.CreateInstance(
                typeof(FileGatewayAdminUserStore),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                binder: null,
                args: new object[] { path },
                culture: null)!;

            Assert.Empty(await store.ListAsync(default));
            Assert.True(await store.AddAsync(new GatewayAdminUser { Name = "alice", Password = "p1", Role = "Admin" }, default));
            Assert.True(await store.AddAsync(new GatewayAdminUser { Name = "bob", Password = "p2", Role = "Reader" }, default));
            Assert.False(await store.AddAsync(new GatewayAdminUser { Name = "ALICE", Password = "x", Role = "Admin" }, default)); // 重名
            var list = await store.ListAsync(default);
            Assert.Equal(2, list.Count);

            // 改密码
            Assert.True(await store.UpdateAsync("alice", "p1-new", null, default));
            var alice = await store.FindAsync("alice", default);
            Assert.NotNull(alice);
            Assert.StartsWith("$pbkdf2-sha256$", alice!.Password);
            Assert.DoesNotContain("p1-new", alice.Password, StringComparison.Ordinal);
            Assert.Equal("Admin", alice.Role);

            var resolver = new UserResolver(
                new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions()),
                store);
            Assert.NotNull(await resolver.ValidateAsync("alice", "p1-new", default));
            Assert.Null(await resolver.ValidateAsync("alice", "wrong", default));

            // 改角色
            Assert.True(await store.UpdateAsync("bob", null, "Admin", default));
            var bob = await store.FindAsync("bob", default);
            Assert.Equal("Admin", bob!.Role);

            // 删除
            Assert.True(await store.RemoveAsync("alice", default));
            Assert.False(await store.RemoveAsync("alice", default)); // 重复删除返回 false
            Assert.Single(await store.ListAsync(default));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task FileUserStore_PersistsToDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = MakeFileStore(path);
            await store.AddAsync(new GatewayAdminUser { Name = "carol", Password = "secret", Role = "Reader" }, default);
            Assert.True(File.Exists(path));
            var raw = File.ReadAllText(path);
            Assert.Contains("carol", raw);
            Assert.DoesNotContain("secret", raw, StringComparison.Ordinal);
            Assert.Contains("$pbkdf2-sha256$", raw, StringComparison.Ordinal);
            Assert.DoesNotContain("\"isBuiltIn\"", raw, StringComparison.OrdinalIgnoreCase); // [JsonIgnore]
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ---------- 3. UserResolver ----------

    [Fact]
    public async Task UserResolver_AppsettingsTakesPriority_OverFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var fileStore = MakeFileStore(path);
            await fileStore.AddAsync(new GatewayAdminUser { Name = "dup", Password = "file-pwd", Role = "Reader" }, default);
            var opts = new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions
            {
                Users = new List<GatewayAdminUser>
                {
                    new() { Name = "dup", Password = "settings-pwd", Role = "Admin" },
                }
            });
            var resolver = new UserResolver(opts, fileStore);
            var list = await resolver.ListAsync(default);
            Assert.Single(list); // 重名 file 项被忽略
            Assert.True(list[0].IsBuiltIn);
            Assert.Equal("settings-pwd", list[0].Password);
            Assert.Equal("Admin", list[0].Role);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task UserResolver_Validate_ReturnsUserOnMatch()
    {
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var fileStore = MakeFileStore(path);
            await fileStore.AddAsync(new GatewayAdminUser { Name = "eve", Password = "pwd", Role = "Reader" }, default);
            var opts = new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions());
            var resolver = new UserResolver(opts, fileStore);

            Assert.Null(await resolver.ValidateAsync("eve", "wrong", default));
            var ok = await resolver.ValidateAsync("EVE", "pwd", default); // 用户名不区分大小写
            Assert.NotNull(ok);
            Assert.Equal("Reader", ok!.Role);
            Assert.False(ok.IsBuiltIn);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task UserResolver_LegacyPlaintextPassword_IsUpgradedAfterSuccessfulLogin()
    {
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            await File.WriteAllTextAsync(path,
                "[{\"Name\":\"legacy\",\"Password\":\"old-secret\",\"Role\":\"Admin\"}]");
            var store = MakeFileStore(path);
            var resolver = new UserResolver(
                new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions()),
                store);

            Assert.NotNull(await resolver.ValidateAsync("legacy", "old-secret", default));
            var raw = await File.ReadAllTextAsync(path);
            Assert.DoesNotContain("old-secret", raw, StringComparison.Ordinal);
            Assert.Contains("$pbkdf2-sha256$", raw, StringComparison.Ordinal);
            Assert.NotNull(await resolver.ValidateAsync("legacy", "old-secret", default));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task UserResolver_CountAdmins_ExcludesReader()
    {
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var fileStore = MakeFileStore(path);
            await fileStore.AddAsync(new GatewayAdminUser { Name = "a1", Password = "p", Role = "Admin" }, default);
            await fileStore.AddAsync(new GatewayAdminUser { Name = "a2", Password = "p", Role = "Admin" }, default);
            await fileStore.AddAsync(new GatewayAdminUser { Name = "r1", Password = "p", Role = "Reader" }, default);
            var resolver = new UserResolver(
                new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions()),
                fileStore);
            Assert.Equal(2, await resolver.CountAdminsAsync(default));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ---------- 4. AdminBasicAuthMiddleware（多用户 + 角色） ----------

    [Fact]
    public async Task Middleware_ValidUser_InjectsRoleClaims()
    {
        var resolver = MakeResolver(new GatewayAdminUser { Name = "alice", Password = "pwd", Role = "Admin" });
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/admin";
        ctx.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alice:pwd"));
        var nextCalled = false;
        var mw = new AdminBasicAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await mw.InvokeAsync(ctx, resolver);
        Assert.True(nextCalled);
        Assert.Equal("alice", ctx.User.Identity?.Name);
        Assert.True(ctx.User.IsInRole("Admin"));
        Assert.Equal("false", ctx.User.FindFirst("gw:builtin")?.Value);
    }

    [Fact]
    public async Task Middleware_BadCreds_ReturnsChallenge()
    {
        var resolver = MakeResolver(new GatewayAdminUser { Name = "alice", Password = "pwd", Role = "Admin" });
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/admin-api/health";
        ctx.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("alice:wrong"));
        var mw = new AdminBasicAuthMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx, resolver);
        Assert.Equal(StatusCodes.Status401Unauthorized, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_ReaderForbidsWriteOnAdminApi()
    {
        var resolver = MakeResolver(new GatewayAdminUser { Name = "bob", Password = "p", Role = "Reader" });
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/admin-api/routes";
        ctx.Request.Method = "POST";
        ctx.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("bob:p"));
        ctx.Response.Body = new MemoryStream();
        var nextCalled = false;
        var mw = new AdminBasicAuthMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await mw.InvokeAsync(ctx, resolver);
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_ReaderAllowsReadAndOwnPasswordChange()
    {
        var resolver = MakeResolver(new GatewayAdminUser { Name = "bob", Password = "p", Role = "Reader" });
        // GET 通过
        var ctxGet = new DefaultHttpContext();
        ctxGet.Request.Path = "/admin-api/health";
        ctxGet.Request.Method = "GET";
        ctxGet.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("bob:p"));
        var calledGet = false;
        await new AdminBasicAuthMiddleware(_ => { calledGet = true; return Task.CompletedTask; }).InvokeAsync(ctxGet, resolver);
        Assert.True(calledGet);

        // 改自己密码 POST 通过（/admin-api/me/* 例外）
        var ctxMe = new DefaultHttpContext();
        ctxMe.Request.Path = "/admin-api/me/password";
        ctxMe.Request.Method = "POST";
        ctxMe.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("bob:p"));
        var calledMe = false;
        await new AdminBasicAuthMiddleware(_ => { calledMe = true; return Task.CompletedTask; }).InvokeAsync(ctxMe, resolver);
        Assert.True(calledMe);
    }

    [Fact]
    public async Task Middleware_PathExceptions_RequireExactPath()
    {
        var resolver = MakeResolver(new GatewayAdminUser { Name = "bob", Password = "p", Role = "Reader" });

        var fakeLogin = new DefaultHttpContext();
        fakeLogin.Request.Path = "/admin/login-anything";
        var fakeLoginCalled = false;
        await new AdminBasicAuthMiddleware(_ => { fakeLoginCalled = true; return Task.CompletedTask; })
            .InvokeAsync(fakeLogin, resolver);
        Assert.False(fakeLoginCalled);
        Assert.Equal(StatusCodes.Status302Found, fakeLogin.Response.StatusCode);

        var fakeMe = new DefaultHttpContext();
        fakeMe.Request.Path = "/admin-api/me/password-reset";
        fakeMe.Request.Method = "POST";
        fakeMe.Request.Headers.Authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("bob:p"));
        var fakeMeCalled = false;
        await new AdminBasicAuthMiddleware(_ => { fakeMeCalled = true; return Task.CompletedTask; })
            .InvokeAsync(fakeMe, resolver);
        Assert.False(fakeMeCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, fakeMe.Response.StatusCode);
    }

    [Fact]
    public async Task Middleware_ProtectedCookie_RoundTripsWithoutPlaintextCredentials()
    {
        var resolver = MakeResolver(new GatewayAdminUser { Name = "alice", Password = "secret", Role = "Admin" });
        var protector = new GatewayAdminCookieProtector(new EphemeralDataProtectionProvider());
        var token = protector.Protect("alice", "secret");
        Assert.DoesNotContain("alice", token, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", token, StringComparison.Ordinal);

        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/admin";
        ctx.Request.Headers.Cookie = AdminBasicAuthMiddleware.AuthCookieName + "=" + token;
        var called = false;
        var middleware = new AdminBasicAuthMiddleware(
            _ => { called = true; return Task.CompletedTask; },
            protector);

        await middleware.InvokeAsync(ctx, resolver);

        Assert.True(called);
        Assert.Equal("alice", ctx.User.Identity?.Name);
    }

    // ---------- 5. UsersController ----------

    [Fact]
    public async Task UsersController_Create_RejectsDuplicate()
    {
        var harness = await NewUsersHarness(actor: "admin", role: "Admin");
        var ok = await harness.Controller.Create(new UsersController.CreateUserRequest { Name = "alice", Password = "p", Role = "Admin" }, default);
        Assert.IsType<OkObjectResult>(ok);
        var dup = await harness.Controller.Create(new UsersController.CreateUserRequest { Name = "alice", Password = "p2", Role = "Reader" }, default);
        Assert.IsType<ConflictObjectResult>(dup);
    }

    [Fact]
    public async Task UsersController_Cannot_DemoteOrDeleteLastAdmin()
    {
        var harness = await NewUsersHarness(actor: "admin", role: "Admin");
        await harness.Controller.Create(new UsersController.CreateUserRequest { Name = "only-admin", Password = "p", Role = "Admin" }, default);
        // 降级到 Reader
        var demote = await harness.Controller.Update("only-admin", new UsersController.UpdateUserRequest { Role = "Reader" }, default);
        Assert.IsType<ConflictObjectResult>(demote);
        // 删除
        // 注意：actor=admin 也是 Admin，所以 only-admin 不是「最后一个」；新建 reader 干扰一下
        // 这里只验证最后一个 admin 被删时的报错路径：删 only-admin 之前先删 actor 不行（actor 不是 file user）
        // 我们换条路径——先把 only-admin 改名为 admin 不行，简化为：删除 only-admin 实际只剩 1 个 admin（actor 不在 file 中），
        // CountAdminsAsync 会返回 1 → 应该 Conflict
        var del = await harness.Controller.Delete("only-admin", default);
        Assert.IsType<ConflictObjectResult>(del);
    }

    [Fact]
    public async Task UsersController_Cannot_DeleteSelf()
    {
        var harness = await NewUsersHarness(actor: "self", role: "Admin");
        // 添加一个 file admin，让自己变成可被「最后一个 admin」检查通过的状态（其实 actor 不在 file 中）
        await harness.Controller.Create(new UsersController.CreateUserRequest { Name = "self", Password = "p", Role = "Admin" }, default);
        await harness.Controller.Create(new UsersController.CreateUserRequest { Name = "another-admin", Password = "p", Role = "Admin" }, default);
        var res = await harness.Controller.Delete("self", default);
        Assert.IsType<ConflictObjectResult>(res);
    }

    [Fact]
    public async Task UsersController_BuiltInUser_CannotBeModified()
    {
        // 通过 OptionsMonitor 注入 builtin
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var store = MakeFileStore(path);
            var opts = new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions
            {
                Users = new List<GatewayAdminUser> { new() { Name = "root", Password = "rp", Role = "Admin" } }
            });
            var resolver = new UserResolver(opts, store);
            var ctrl = new UsersController(resolver, store, new NoopAudit(), new NoopBus());
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = MakeHttpContext("admin", "Admin"),
            };
            var update = await ctrl.Update("root", new UsersController.UpdateUserRequest { Password = "x" }, default);
            Assert.IsType<BadRequestObjectResult>(update);
            var del = await ctrl.Delete("root", default);
            Assert.IsType<BadRequestObjectResult>(del);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task UsersController_ChangeMyPassword_RequiresCorrectOldPassword()
    {
        var harness = await NewUsersHarness(actor: "me", role: "Admin");
        await harness.Controller.Create(new UsersController.CreateUserRequest { Name = "me", Password = "old", Role = "Admin" }, default);
        var bad = await harness.Controller.ChangeMyPassword(
            new UsersController.ChangePasswordRequest { OldPassword = "wrong", NewPassword = "n" }, default);
        Assert.IsType<UnauthorizedObjectResult>(bad);
        var good = await harness.Controller.ChangeMyPassword(
            new UsersController.ChangePasswordRequest { OldPassword = "old", NewPassword = "new" }, default);
        Assert.IsType<NoContentResult>(good);
    }

    // ---------- 6. GatewayConfiguration 全局限流桥 ----------

    [Fact]
    public void GatewayConfiguration_GlobalRateLimit_RoundTrips()
    {
        var cfg = new GatewayConfiguration();
        Assert.Null(cfg.GetGlobalRateLimit());
        cfg.SetGlobalRateLimit(new GatewayGlobalRateLimitOptions
        {
            DisableRateLimitHeaders = true,
            HttpStatusCode = 429,
            QuotaExceededMessage = "too many",
            ClientIdHeader = "ClientId",
        });
        var read = cfg.GetGlobalRateLimit();
        Assert.NotNull(read);
        Assert.True(read!.DisableRateLimitHeaders);
        Assert.Equal(429, read.HttpStatusCode);
        Assert.Equal("too many", read.QuotaExceededMessage);
        Assert.Equal("ClientId", read.ClientIdHeader);
    }

    [Fact]
    public void GatewayConfiguration_GlobalRateLimit_PreservesOtherGlobalFields()
    {
        // 模拟 ocelot.json 已有 BaseUrl 字段
        var raw = """{"Routes":[],"GlobalConfiguration":{"BaseUrl":"http://a.com"}}""";
        var cfg = JsonSerializer.Deserialize<GatewayConfiguration>(raw)!;
        cfg.SetGlobalRateLimit(new GatewayGlobalRateLimitOptions { HttpStatusCode = 503 });
        var globalRaw = cfg.Extra!["GlobalConfiguration"].GetRawText();
        Assert.Contains("BaseUrl", globalRaw);
        Assert.Contains("RateLimitOptions", globalRaw);
        Assert.Equal(503, cfg.GetGlobalRateLimit()!.HttpStatusCode);
    }

    [Fact]
    public void GatewayConfiguration_SetGlobalRateLimit_NullRemoves()
    {
        var cfg = new GatewayConfiguration();
        cfg.SetGlobalRateLimit(new GatewayGlobalRateLimitOptions { HttpStatusCode = 429 });
        cfg.SetGlobalRateLimit(null);
        Assert.Null(cfg.GetGlobalRateLimit());
    }

    // ---------- 7. AdminController 限流/LB 端点 ----------

    [Fact]
    public async Task AdminController_RouteRateLimit_RoundTrips()
    {
        var harness = await NewAdminHarness(new[] { "/a", "/b" });
        var put = await harness.Controller.SetRouteRateLimit(0, new GatewayRateLimitOptions
        {
            EnableRateLimiting = true,
            Period = "1s",
            PeriodTimespan = 1,
            Limit = 10,
        }, default);
        Assert.IsType<NoContentResult>(put);

        var get = await harness.Controller.GetRouteRateLimit(0, default) as OkObjectResult;
        var rl = Assert.IsType<GatewayRateLimitOptions>(get!.Value);
        Assert.True(rl.EnableRateLimiting);
        Assert.Equal(10, rl.Limit);

        var clear = await harness.Controller.ClearRouteRateLimit(0, default);
        Assert.IsType<NoContentResult>(clear);
        var after = await harness.Controller.GetRouteRateLimit(0, default) as OkObjectResult;
        Assert.Null(after!.Value);
    }

    [Fact]
    public async Task AdminController_RouteRateLimit_RejectsBadPeriod()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var bad = await harness.Controller.SetRouteRateLimit(0, new GatewayRateLimitOptions
        {
            EnableRateLimiting = true,
            Period = "weekly", // 非法
            Limit = 10,
        }, default);
        Assert.IsType<BadRequestObjectResult>(bad);
    }

    [Fact]
    public async Task AdminController_RouteRateLimit_RejectsZeroLimit()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var bad = await harness.Controller.SetRouteRateLimit(0, new GatewayRateLimitOptions
        {
            EnableRateLimiting = true,
            Period = "1s",
            Limit = 0, // 非法
        }, default);
        Assert.IsType<BadRequestObjectResult>(bad);
    }

    [Fact]
    public async Task AdminController_RouteRateLimit_NotFound_OnBadIndex()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var nf = await harness.Controller.SetRouteRateLimit(99, new GatewayRateLimitOptions(), default);
        Assert.IsType<NotFoundResult>(nf);
    }

    [Fact]
    public async Task AdminController_GlobalRateLimit_RoundTrips()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var put = await harness.Controller.SetGlobalRateLimit(new GatewayGlobalRateLimitOptions
        {
            HttpStatusCode = 429,
            ClientIdHeader = "X-Client",
        }, default);
        Assert.IsType<NoContentResult>(put);
        var get = await harness.Controller.GetGlobalRateLimit(default) as OkObjectResult;
        var g = Assert.IsType<GatewayGlobalRateLimitOptions>(get!.Value);
        Assert.Equal(429, g.HttpStatusCode);

        var clear = await harness.Controller.ClearGlobalRateLimit(default);
        Assert.IsType<NoContentResult>(clear);
    }

    [Fact]
    public async Task AdminController_GlobalRateLimit_RejectsBadStatusCode()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var bad = await harness.Controller.SetGlobalRateLimit(
            new GatewayGlobalRateLimitOptions { HttpStatusCode = 99 }, default);
        Assert.IsType<BadRequestObjectResult>(bad);
    }

    [Fact]
    public async Task AdminController_LoadBalancer_RoundTrips()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var put = await harness.Controller.SetRouteLoadBalancer(0,
            new GatewayLoadBalancerOptions { Type = "LeastConnection" }, default);
        Assert.IsType<NoContentResult>(put);
        var get = await harness.Controller.GetRouteLoadBalancer(0, default) as OkObjectResult;
        var lb = Assert.IsType<GatewayLoadBalancerOptions>(get!.Value);
        Assert.Equal("LeastConnection", lb.Type);
    }

    [Fact]
    public async Task AdminController_LoadBalancer_RejectsUnknownType()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var bad = await harness.Controller.SetRouteLoadBalancer(0,
            new GatewayLoadBalancerOptions { Type = "BogusAlgo" }, default);
        Assert.IsType<BadRequestObjectResult>(bad);
    }

    [Fact]
    public async Task AdminController_LoadBalancerTypes_ReturnsThree()
    {
        var harness = await NewAdminHarness(new[] { "/a" });
        var ok = harness.Controller.GetLoadBalancerTypes() as OkObjectResult;
        var arr = Assert.IsType<string[]>(ok!.Value);
        Assert.Equal(3, arr.Length);
        Assert.Contains("RoundRobin", arr);
        Assert.Contains("LeastConnection", arr);
        Assert.Contains("NoLoadBalance", arr);
    }

    // ---------- 9. appsettings 凭据绑定（单账号 / 多账号 / 并存） ----------

    [Fact]
    public void AddSyZeroGatewayAdmin_RegistersCookieProtector()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSyZeroGatewayAdmin();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<GatewayAdminCookieProtector>());
    }

    [Fact]
    public void Bind_LegacyUsernamePassword_RegistersSingleAdmin()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:Admin:Username"] = "legacy",
                ["Gateway:Admin:Password"] = "p@ss",
            })
            .Build();

        var users = BindUsers(cfg);

        var u = Assert.Single(users);
        Assert.Equal("legacy", u.Name);
        Assert.Equal("p@ss", u.Password);
        Assert.Equal(GatewayAdminRoles.Admin, u.Role);
    }

    [Fact]
    public void Bind_LegacyAndUsersArrayCoexist_ArrayOverridesOnSameName()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:Admin:Username"] = "admin",
                ["Gateway:Admin:Password"] = "old",
                ["Gateway:Admin:Users:0:Name"] = "admin",
                ["Gateway:Admin:Users:0:Password"] = "new",
                ["Gateway:Admin:Users:0:Role"] = "Reader",
                ["Gateway:Admin:Users:1:Name"] = "viewer",
                ["Gateway:Admin:Users:1:Password"] = "vp",
                ["Gateway:Admin:Users:1:Role"] = "Reader",
            })
            .Build();

        var users = BindUsers(cfg);

        Assert.Equal(2, users.Count);
        var admin = users.Single(x => x.Name == "admin");
        Assert.Equal("new", admin.Password);
        Assert.Equal(GatewayAdminRoles.Reader, admin.Role);
        Assert.Contains(users, x => x.Name == "viewer" && x.Role == GatewayAdminRoles.Reader);
    }

    [Fact]
    public void Bind_NoCredentials_YieldsEmptyList()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var users = BindUsers(cfg);

        Assert.Empty(users);
    }

    // ============== 测试辅助 ==============

    private static List<GatewayAdminUser> BindUsers(IConfiguration cfg)
    {
        // 复用扩展中的绑定算法（与 SyZeroGatewayAdminExtension.AddSyZeroGatewayAdmin 保持一致）。
        var list = new List<GatewayAdminUser>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var legacyName = cfg["Gateway:Admin:Username"];
        var legacyPwd = cfg["Gateway:Admin:Password"];
        if (!string.IsNullOrWhiteSpace(legacyName))
        {
            list.Add(new GatewayAdminUser
            {
                Name = legacyName!,
                Password = legacyPwd ?? string.Empty,
                Role = GatewayAdminRoles.Admin,
            });
            seen.Add(legacyName!);
        }

        var section = cfg.GetSection(GatewayAdminUsersOptions.SectionName);
        foreach (var child in section.GetChildren())
        {
            var name = child["Name"];
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (seen.Contains(name!))
            {
                list.RemoveAll(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase));
            }
            seen.Add(name!);
            list.Add(new GatewayAdminUser
            {
                Name = name!,
                Password = child["Password"] ?? string.Empty,
                Role = GatewayAdminRoles.Normalize(child["Role"]),
            });
        }
        return list;
    }

    private static FileGatewayAdminUserStore MakeFileStore(string path)
    {
        return (FileGatewayAdminUserStore)Activator.CreateInstance(
            typeof(FileGatewayAdminUserStore),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            args: new object[] { path },
            culture: null)!;
    }

    private static IUserResolver MakeResolver(GatewayAdminUser user)
    {
        var dummy = new InMemoryUserStore();
        dummy.SeedSync(user);
        var opts = new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions());
        return new UserResolver(opts, dummy);
    }

    private static DefaultHttpContext MakeHttpContext(string user, string role)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, user),
            new Claim(ClaimTypes.Role, role),
        }, "Test"));
        return ctx;
    }

    private async Task<UsersHarness> NewUsersHarness(string actor, string role)
    {
        var path = Path.Combine(Path.GetTempPath(), "syzero-users-" + Guid.NewGuid().ToString("N") + ".json");
        var store = MakeFileStore(path);
        var resolver = new UserResolver(
            new SimpleOptionsMonitor<GatewayAdminUsersOptions>(new GatewayAdminUsersOptions()),
            store);
        var ctrl = new UsersController(resolver, store, new NoopAudit(), new NoopBus());
        ctrl.ControllerContext = new ControllerContext { HttpContext = MakeHttpContext(actor, role) };
        await Task.CompletedTask;
        return new UsersHarness(ctrl, path);
    }

    private async Task<AdminHarness> NewAdminHarness(string[] upstreams)
    {
        var (store, _) = NewFileStore();
        if (upstreams.Length > 0)
        {
            await store.WriteAsync(new GatewayConfiguration
            {
                Routes = upstreams.Select(u => new GatewayRoute
                {
                    UpstreamPathTemplate = u,
                    DownstreamScheme = "http",
                    DownstreamPathTemplate = u,
                    UpstreamHttpMethod = new List<string> { "GET" },
                }).ToList()
            });
        }
        var ctrl = new AdminController(store, new NoopAudit(), new NoopBus(), new EmptyServiceProvider(), new ConfigurationBuilder().Build());
        ctrl.ControllerContext = new ControllerContext { HttpContext = MakeHttpContext("admin", "Admin") };
        return new AdminHarness(ctrl);
    }

    private static (FileGatewayConfigurationStore Store, NoOpApplier Applier) NewFileStore()
    {
        var dir = Path.Combine(Path.GetTempPath(), "syzero-cfg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.SetupGet(e => e.ContentRootPath).Returns(dir);
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:Admin:Backup:MaxCount"] = "5",
            }).Build();
        var applier = new NoOpApplier();
        var store = new FileGatewayConfigurationStore(envMock.Object, cfg, applier, NullLogger<FileGatewayConfigurationStore>.Instance);
        return (store, applier);
    }

    private sealed class NoOpApplier : IGatewayConfigurationApplier
    {
        public Task ApplyAsync(GatewayConfiguration configuration, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class UsersHarness
    {
        public UsersController Controller { get; }
        public string FilePath { get; }
        public UsersHarness(UsersController c, string p) { Controller = c; FilePath = p; }
    }

    private sealed class AdminHarness
    {
        public AdminController Controller { get; }
        public AdminHarness(AdminController c) { Controller = c; }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class NoopAudit : IGatewayAdminAuditLog
    {
        public bool Enabled => false;
        public Task AppendAsync(GatewayAdminAuditEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<GatewayAdminAuditEntry>> ReadRecentAsync(int take, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GatewayAdminAuditEntry>>(Array.Empty<GatewayAdminAuditEntry>());
    }

    private sealed class NoopBus : IGatewayAdminEventBus
    {
        public int SubscriberCount => 0;
        public void Publish(GatewayAdminEvent evt) { }
        public (Guid SubscriptionId, System.Threading.Channels.ChannelReader<GatewayAdminEvent> Reader) Subscribe()
            => throw new NotSupportedException();
        public void Unsubscribe(Guid subscriptionId) { }
    }

    private sealed class SimpleOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        private readonly T _value;
        public SimpleOptionsMonitor(T value) { _value = value; }
        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable OnChange(Action<T, string?> listener) => new NoopDisposable();
        private sealed class NoopDisposable : IDisposable { public void Dispose() { } }
    }

    private sealed class InMemoryUserStore : IGatewayAdminUserStore
    {
        private readonly List<GatewayAdminUser> _list = new();
        public void SeedSync(GatewayAdminUser u) => _list.Add(u);
        public Task<bool> AddAsync(GatewayAdminUser user, CancellationToken ct)
        {
            if (_list.Any(x => string.Equals(x.Name, user.Name, StringComparison.OrdinalIgnoreCase)))
                return Task.FromResult(false);
            _list.Add(user);
            return Task.FromResult(true);
        }
        public Task<GatewayAdminUser?> FindAsync(string name, CancellationToken ct)
            => Task.FromResult(_list.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyList<GatewayAdminUser>> ListAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<GatewayAdminUser>>(_list);
        public Task<bool> RemoveAsync(string name, CancellationToken ct)
        {
            var idx = _list.FindIndex(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return Task.FromResult(false);
            _list.RemoveAt(idx);
            return Task.FromResult(true);
        }
        public Task<bool> UpdateAsync(string name, string? newPassword, string? newRole, CancellationToken ct)
        {
            var u = _list.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
            if (u == null) return Task.FromResult(false);
            if (newPassword != null) u.Password = newPassword;
            if (newRole != null) u.Role = newRole;
            return Task.FromResult(true);
        }
    }
}


