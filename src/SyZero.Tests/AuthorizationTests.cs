using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SyZero;
using SyZero.Authorization.Entities;
using SyZero.Authorization.Jwt;
using SyZero.Authorization.Permissions;
using SyZero.Domain.Entities;
using SyZero.Domain.Repository;
using SyZero.EntityFrameworkCore;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;
using Xunit;
namespace SyZero.Tests;

[Collection("AppConfig")]
public class AuthorizationTests
{
    [Fact]
    public void JwtTokenManager_RoundTrip_ReturnsClaims()
    {
        var opt = Options.Create(new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAud",
            SecretKey = "this-is-a-very-long-secret-key-for-hmac-256-test-only",
            AccessTokenExpirationMinutes = 30
        });
        var manager = new JwtTokenManager(opt);

        var claims = new[]
        {
            new Claim(SyClaimTypes.UserId, "12345"),
            new Claim(SyClaimTypes.UserName, "alice")
        };
        var token = manager.CreateAccessToken(claims);
        Assert.False(string.IsNullOrEmpty(token));

        var principal = manager.GetPrincipal(token);
        Assert.NotNull(principal);
        Assert.Equal("12345", principal!.FindFirst(SyClaimTypes.UserId)?.Value);
        Assert.Equal("alice", principal.FindFirst(SyClaimTypes.UserName)?.Value);
    }

    [Fact]
    public void JwtTokenManager_GetPrincipal_InvalidToken_ReturnsNull()
    {
        var opt = Options.Create(new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAud",
            SecretKey = "this-is-a-very-long-secret-key-for-hmac-256-test-only"
        });
        var manager = new JwtTokenManager(opt);
        Assert.Null(manager.GetPrincipal("not-a-jwt"));
    }

    [Fact]
    public void PermissionDefinitionManager_AggregatesProviders()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPermissionDefinitionProvider, FakePermissionProvider>();
        services.AddSingleton<PermissionDefinitionManager>();
        using var sp = services.BuildServiceProvider();

        var manager = sp.GetRequiredService<PermissionDefinitionManager>();
        Assert.True(manager.Groups.Count >= 1);
        Assert.NotNull(manager.Get("Test.Users.Create"));
        Assert.NotNull(manager.Get("Test.Users"));
        Assert.Null(manager.Get("Nonexistent.Perm"));
    }

    [Fact]
    public void RbacPermissionChecker_NoUser_ReturnsFalse()
    {
        using var db = new AuthDb();
        using var sp = BuildAuthProvider(db.ConnectionString, userId: null);
        using var scope = sp.CreateScope();
        var checker = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();
        Assert.False(checker.IsHas("Test.Users.Create"));
    }

    [Fact]
    public async Task RbacPermissionChecker_RoleGranted_ReturnsTrue()
    {
        using var db = new AuthDb();
        using var sp = BuildAuthProvider(db.ConnectionString, userId: 100L);

        // 种数据：用户 100 → 角色 1 → 授予 Test.Users.Create
        using (var seed = sp.CreateScope())
        {
            var ur = seed.ServiceProvider.GetRequiredService<IRepository<UserRole>>();
            var rp = seed.ServiceProvider.GetRequiredService<IRepository<RolePermission>>();
            await ur.AddAsync(new UserRole { UserId = 100L, RoleId = 1L });
            await rp.AddAsync(new RolePermission { RoleId = 1L, PermissionName = "Test.Users.Create", IsGranted = true });
        }

        using var scope = sp.CreateScope();
        var checker = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();
        Assert.True(checker.IsHas("Test.Users.Create"));
        Assert.False(checker.IsHas("Test.Users.Delete"));
    }

    [Fact]
    public async Task RbacPermissionChecker_UserOverride_DenyWins()
    {
        using var db = new AuthDb();
        using var sp = BuildAuthProvider(db.ConnectionString, userId: 200L);

        using (var seed = sp.CreateScope())
        {
            var ur = seed.ServiceProvider.GetRequiredService<IRepository<UserRole>>();
            var rp = seed.ServiceProvider.GetRequiredService<IRepository<RolePermission>>();
            var up = seed.ServiceProvider.GetRequiredService<IRepository<UserPermission>>();
            await ur.AddAsync(new UserRole { UserId = 200L, RoleId = 2L });
            await rp.AddAsync(new RolePermission { RoleId = 2L, PermissionName = "Test.Users.Delete", IsGranted = true });
            // 用户级显式拒绝覆盖角色级允许
            await up.AddAsync(new UserPermission { UserId = 200L, PermissionName = "Test.Users.Delete", IsGranted = false });
        }

        using var scope = sp.CreateScope();
        var checker = scope.ServiceProvider.GetRequiredService<IPermissionChecker>();
        Assert.False(checker.IsHas("Test.Users.Delete"));
    }

    [Fact]
    public async Task RefreshTokenManager_IssueAndRotate_Works()
    {
        using var db = new AuthDb();
        using var sp = BuildAuthProviderWithRefresh(db.ConnectionString);

        var claims = new[] { new Claim(SyClaimTypes.UserId, "1001") };
        TokenPairResult pair;
        using (var scope = sp.CreateScope())
        {
            var mgr = scope.ServiceProvider.GetRequiredService<IRefreshTokenManager>();
            pair = await mgr.IssueAsync(1001L, claims);
            Assert.False(string.IsNullOrEmpty(pair.AccessToken));
            Assert.False(string.IsNullOrEmpty(pair.RefreshToken));
        }

        TokenPairResult rotated;
        using (var scope = sp.CreateScope())
        {
            var mgr = scope.ServiceProvider.GetRequiredService<IRefreshTokenManager>();
            rotated = await mgr.RotateAsync(pair.RefreshToken, claims);
            Assert.NotEqual(pair.RefreshToken, rotated.RefreshToken);
        }

        // 旧 token 不能再用
        using (var scope = sp.CreateScope())
        {
            var mgr = scope.ServiceProvider.GetRequiredService<IRefreshTokenManager>();
            var ex = await Assert.ThrowsAsync<SyMessageException>(() => mgr.RotateAsync(pair.RefreshToken, claims));
            Assert.Contains("撤销", ex.Message);
        }

        // 级联撤销后，rotated 也应被撤销
        using (var scope = sp.CreateScope())
        {
            var mgr = scope.ServiceProvider.GetRequiredService<IRefreshTokenManager>();
            await Assert.ThrowsAsync<SyMessageException>(() => mgr.RotateAsync(rotated.RefreshToken, claims));
        }
    }

    [Fact]
    public async Task RefreshTokenManager_RevokeAll_DisablesAll()
    {
        using var db = new AuthDb();
        using var sp = BuildAuthProviderWithRefresh(db.ConnectionString);

        var claims = new[] { new Claim(SyClaimTypes.UserId, "2002") };
        TokenPairResult p1, p2;
        using (var scope = sp.CreateScope())
        {
            var mgr = scope.ServiceProvider.GetRequiredService<IRefreshTokenManager>();
            p1 = await mgr.IssueAsync(2002L, claims);
            p2 = await mgr.IssueAsync(2002L, claims);
        }

        using (var scope = sp.CreateScope())
        {
            var mgr = scope.ServiceProvider.GetRequiredService<IRefreshTokenManager>();
            await mgr.RevokeAllAsync(2002L);
        }

        using (var scope = sp.CreateScope())
        {
            var mgr = scope.ServiceProvider.GetRequiredService<IRefreshTokenManager>();
            await Assert.ThrowsAsync<SyMessageException>(() => mgr.RotateAsync(p1.RefreshToken, claims));
            await Assert.ThrowsAsync<SyMessageException>(() => mgr.RotateAsync(p2.RefreshToken, claims));
        }
    }

    private static ServiceProvider BuildAuthProviderWithRefresh(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSyZeroEntityFramework<AuthTestDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ISySession>(_ => new FakeSession(null));
        services.Configure<JwtOptions>(o =>
        {
            o.Issuer = "T"; o.Audience = "T";
            o.SecretKey = "this-is-a-very-long-secret-key-for-hmac-256-test-only";
            o.RefreshTokenExpirationDays = 7;
        });
        services.AddSingleton<IToken, JwtTokenManager>();
        services.AddScoped<IRefreshTokenManager, RefreshTokenManager>();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuthTestDbContext>();
            ctx.Database.EnsureCreated();
        }
        return provider;
    }

    private static ServiceProvider BuildAuthProvider(string connectionString, long? userId)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSyZeroEntityFramework<AuthTestDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ISySession>(_ => new FakeSession(userId));
        services.AddScoped<IPermissionChecker, RbacPermissionChecker>();

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        using (var scope = provider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<AuthTestDbContext>();
            ctx.Database.EnsureCreated();
        }

        return provider;
    }

    private sealed class FakePermissionProvider : IPermissionDefinitionProvider
    {
        public void Define(PermissionDefinitionContext ctx)
        {
            var g = ctx.AddGroup("Test", "测试组");
            var users = g.AddPermission("Test.Users", "用户管理");
            users.AddChild("Test.Users.Create", "新增");
            users.AddChild("Test.Users.Delete", "删除");
        }
    }

    private sealed class FakeSession : ISySession
    {
        public FakeSession(long? userId) { UserId = userId; }
        public ClaimsPrincipal? Principal => null;
        public long? UserId { get; }
        public string? UserRole => null;
        public string? UserName => null;
        public List<string>? Permission => null;
        public string? Token => null;
        public ISySession Parse(ClaimsPrincipal claimsPrincipal) => this;
        public ISySession Parse(string token) => this;
    }

    public sealed class AuthTestDbContext : SyZeroDbContext<AuthTestDbContext>
    {
        public AuthTestDbContext(DbContextOptions<AuthTestDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<PermissionGroup> PermissionGroups => Set<PermissionGroup>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    }

    private sealed class AuthDb : IDisposable
    {
        public AuthDb()
        {
            FilePath = Path.Combine(Path.GetTempPath(), $"syzero-auth-{Guid.NewGuid():N}.db");
            ConnectionString = $"Data Source={FilePath}";
        }
        public string ConnectionString { get; }
        private string FilePath { get; }
        public void Dispose()
        {
            if (File.Exists(FilePath))
            {
                try { File.Delete(FilePath); } catch (IOException) { }
            }
        }
    }
}
