# SyZero.Authorization

SyZero 鉴权 / 授权模块，提供 **ASP.NET Core Identity（基于 SyZero 仓储的 IUserStore/IRoleStore）+ ABP 风格 RBAC（PermissionGroup/Permission/Role/UserRole/RolePermission/UserPermission）+ JWT Bearer** 的开箱即用方案。

## 设计概览

| 能力 | 实现 |
|---|---|
| 用户/角色 | `User` / `Role` 实体 + `SyZeroUserStore` / `SyZeroRoleStore`（实现 IUserStore / IRoleStore，存储走 SyZero `IRepository<T>`） |
| 权限定义 | 代码式 `IPermissionDefinitionProvider` + `PermissionGroupDefinition` / `PermissionDefinition`（树形） |
| 权限存储 | `PermissionGroup` / `Permission` 实体；启动时由 `PermissionSyncHostedService` 自动同步代码定义到数据库 |
| 权限授予 | 角色级 `RolePermission` + 用户级 `UserPermission`（用户级覆盖角色级；任一显式拒绝即拒） |
| 权限检查 | `RbacPermissionChecker` 替换 SyZero.Core 的 `DefaultPermissionChecker`（透明）；业务沿用 `[CheckPermission]` / `IPermissionChecker.Authorize(...)` |
| 令牌 | `JwtTokenManager` 实现 `IToken`（HMAC-SHA256），通过 `JwtOptions` 配置；ASP.NET Core JwtBearer 鉴权一并接入 |

## 依赖

- `SyZero.Core`
- `Microsoft.AspNetCore.App`（FrameworkReference，提供 Identity Manager / JwtBearer）
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`

需要业务侧另选一个 SyZero 仓储 Provider（如 `SyZero.EntityFrameworkCore` / `SyZero.SqlSugar`）来注册 `IRepository<>`。

## 快速接入

```csharp
// Program.cs
builder.Services
    .AddSyZero()
    .AddSyZeroEntityFrameworkCore<MyDbContext>(opt => opt.UseSqlite("Data Source=app.db"))
    .AddSyZeroAuthorization(builder.Configuration, jwt =>
    {
        jwt.Issuer = "MyApp";
        jwt.Audience = "MyApp";
        jwt.SecretKey = builder.Configuration["Jwt:SecretKey"]!;
    });

var app = builder.Build();
app.UseRouting();
app.UseSyZeroAuthorization(); // = UseAuthentication + UseAuthorization
app.MapControllers();
app.Run();
```

业务侧 `DbContext` 需包含 `DbSet<User>`、`DbSet<Role>`、`DbSet<UserRole>`、`DbSet<RolePermission>`、`DbSet<UserPermission>`、`DbSet<PermissionGroup>`、`DbSet<Permission>`。

## 定义权限

```csharp
public class MyAppPermissionProvider : IPermissionDefinitionProvider
{
    public void Define(PermissionDefinitionContext ctx)
    {
        var g = ctx.AddGroup("MyApp", "我的应用");
        var users = g.AddPermission("MyApp.Users", "用户管理");
        users.AddChild("MyApp.Users.Create", "新增用户");
        users.AddChild("MyApp.Users.Update", "更新用户");
        users.AddChild("MyApp.Users.Delete", "删除用户");
    }
}

// 注册（自动 DI 不会扫到 Provider，需手动注册）
builder.Services.AddSingleton<IPermissionDefinitionProvider, MyAppPermissionProvider>();
```

启动时 `PermissionSyncHostedService` 会自动把上述定义 Upsert 到 `PermissionGroup` / `Permission` 表。

## 业务侧检查权限

```csharp
public class UserAppService : SyZeroServiceBase, IUserAppService
{
    public async Task CreateAsync(CreateUserDto input)
    {
        CheckPermission("MyApp.Users.Create"); // 无权限抛 SyMessageException(NoPermission)
        // ...
    }
}
```

## JWT 配置示例

`appsettings.json`：

```json
{
  "Jwt": {
    "Issuer": "MyApp",
    "Audience": "MyApp",
    "SecretKey": "替换为足够长的随机字符串（>=32 字符）",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

签发示例：

```csharp
var claims = new[]
{
    new Claim(SyClaimTypes.UserId, user.Id.ToString()),
    new Claim(SyClaimTypes.UserName, user.UserName)
};
var accessToken = tokenManager.CreateAccessToken(claims); // tokenManager: IToken
```

## 数据库迁移要点（EF Core 示例）

```csharp
modelBuilder.Entity<User>().HasIndex(u => u.NormalizedUserName).IsUnique();
modelBuilder.Entity<Role>().HasIndex(r => r.NormalizedName).IsUnique();
modelBuilder.Entity<UserRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.PermissionName }).IsUnique();
modelBuilder.Entity<UserPermission>().HasIndex(x => new { x.UserId, x.PermissionName }).IsUnique();
modelBuilder.Entity<Permission>().HasIndex(p => p.Name).IsUnique();
modelBuilder.Entity<PermissionGroup>().HasIndex(g => g.Name).IsUnique();
```

## 扩展点

- 替换密码哈希算法：`services.AddScoped<IPasswordHasher<User>, MyHasher>();`
- 替换 `IToken`：移除默认 `JwtTokenManager` 注册，自定义实现并 `AddSingleton<IToken, MyToken>()`。
- 替换 `IPermissionChecker`：在 `AddSyZeroAuthorization` 之后用 `services.Replace(...)` 覆盖。

## 兼容性

- TargetFramework：`net9.0`
- 主键：固定 `long`（与 SyZero 雪花 ID 一致）。如需自定义主键，请使用泛型仓储 `IRepository<TEntity, TKey>`（自 1.3.0 起 `SyZero.Core` 已提供）自行扩展。

## RefreshToken（脱机刷新令牌）

模块内置 `IRefreshTokenManager` + `RefreshTokenManager` + `RefreshToken` 实体（**已纳入 `AddSyZeroAuthorization()` 自动注册**）。

设计要点：

| 项目 | 取值 |
|---|---|
| 原始 Token | 32 字节 `RandomNumberGenerator` + Base64Url，**只在签发/轮转时返回一次** |
| DB 存储 | 仅 SHA-256 哈希（即使数据库泄漏也无法直接复用） |
| 过期 | `JwtOptions.RefreshTokenExpirationDays`（默认 7 天） |
| 轮转 | 每次 `RotateAsync` 旧 token 立刻置 `RevokedAt` 并写入 `ReplacedByTokenHash`（轮转链） |
| 复用检测 | 已撤销的 token 再次提交 → 视为令牌泄漏，**自动级联撤销该用户全部活跃 RefreshToken**，调用方需重新登录 |
| 主动撤销 | `RevokeAsync(rawToken)` 单条；`RevokeAllAsync(userId)` 全员（用于改密 / 强制下线） |

### 数据库

```csharp
modelBuilder.Entity<RefreshToken>().HasIndex(t => t.TokenHash).IsUnique();
modelBuilder.Entity<RefreshToken>().HasIndex(t => new { t.UserId, t.RevokedAt });
```

### 在 AppService 中使用

```csharp
public class AuthAppService : SyZeroServiceBase, IAuthAppService
{
    private readonly UserManager<User> _userManager;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IRefreshTokenManager _refresh;

    public AuthAppService(
        UserManager<User> userManager,
        IPasswordHasher<User> hasher,
        IRefreshTokenManager refresh)
    {
        _userManager = userManager;
        _hasher = hasher;
        _refresh = refresh;
    }

    public async Task<TokenPairResult> LoginAsync(LoginDto input)
    {
        var user = await _userManager.FindByNameAsync(input.UserName)
            ?? throw new SyMessageException("用户不存在");
        if (_hasher.VerifyHashedPassword(user, user.PasswordHash!, input.Password) == PasswordVerificationResult.Failed)
            throw new SyMessageException("密码错误");

        var claims = new[]
        {
            new Claim(SyClaimTypes.UserId, user.Id.ToString()),
            new Claim(SyClaimTypes.UserName, user.UserName!)
        };
        return await _refresh.IssueAsync(user.Id, claims, createdByIp: null);
    }

    public Task<TokenPairResult> RefreshAsync(string refreshToken)
    {
        // 注意：claims 应从 user 重新组装；这里简化为同一 set
        // 真实业务建议先用旧 access_token 解析出 userId 再重组 claims
        return _refresh.RotateAsync(refreshToken, /* claims */ Array.Empty<Claim>());
    }

    public Task LogoutAsync(string refreshToken) => _refresh.RevokeAsync(refreshToken);
}
```

### 安全建议

- 在控制器层将 `refresh_token` 放入 **HttpOnly + Secure + SameSite=Strict Cookie**，前端只持有短期 `access_token`；
- 改密、邮箱变更、检测到异地登录时调用 `RevokeAllAsync(userId)` 强制全终端下线；
- 若 `RotateAsync` 抛出含「撤销」字样的 `SyMessageException`，应清除客户端全部令牌并要求重新登录（已发生疑似令牌泄漏）。

## 账户便捷操作（SyZeroAccountManager）

`SyZeroAccountManager` 是把 `UserManager<User>` 与 `IRefreshTokenManager` 组合在一起的 **薄粘合层**，带 `IScopedDependency` 自动注册。它的目标：让「登出 / 全终端登出 / 改密 / 重置密码」这些会影响令牌生命周期的操作，**保证刷新令牌一并失效**，避免业务侧忘记调用 `RevokeAllAsync`。

| 方法 | 行为 | 典型场景 |
| --- | --- | --- |
| `LogoutAsync(refreshToken, ct)` | 仅撤销当前设备的 refresh_token | 单设备退出 |
| `LogoutAllAsync(userId, ct)` | 撤销该用户全部 refresh_token | 「在其他设备退出」、检测到异常登录 |
| `ChangePasswordAsync(userId, current, new, ct)` | 走 `UserManager.ChangePasswordAsync`，**成功后**自动 `RevokeAllAsync` | 用户主动改密 |
| `ResetPasswordAsync(userId, new, ct)` | 管理员重置：内部生成 reset token 后立即 `ResetPasswordAsync`；**成功后**自动 `RevokeAllAsync` | 管理员强制重置 / 忘记密码流程 |

> 改密 / 重置密码成功后，所有终端将在下一次刷新令牌时被强制下线（旧 access_token 仍可用至自然过期，建议把 `AccessTokenExpirationMinutes` 设短）。

### 控制器示例

```csharp
public class AccountAppService : SyZeroServiceBase, IAccountAppService
{
    private readonly SyZeroAccountManager _account;
    private readonly ISySession _session;

    public AccountAppService(SyZeroAccountManager account, ISySession session)
    {
        _account = account;
        _session = session;
    }

    public Task LogoutAsync(string refreshToken)
        => _account.LogoutAsync(refreshToken);

    public Task LogoutAllAsync()
        => _account.LogoutAllAsync(_session.UserId!.Value);

    public async Task ChangePasswordAsync(ChangePasswordDto dto)
    {
        var result = await _account.ChangePasswordAsync(
            _session.UserId!.Value, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new SyMessageException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
```

## 完整可复制示例：注册 / 登录 / 受保护接口

### 1) `IApplication` — 接口与 DTO

```csharp
// IMyApp.IApplication/Auth/IAuthAppService.cs
using SyZero.Application.Service;

namespace MyApp.IApplication.Auth;

public class RegisterDto
{
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? Email { get; set; }
}

public class LoginDto
{
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class LoginResultDto
{
    public string AccessToken { get; set; } = default!;
    public long UserId { get; set; }
    public string UserName { get; set; } = default!;
}

public interface IAuthAppService : IApplicationService
{
    Task<long> RegisterAsync(RegisterDto input);
    Task<LoginResultDto> LoginAsync(LoginDto input);
    Task<string> WhoAmIAsync();
}
```

### 2) `Application` — 实现

```csharp
// MyApp.Application/Auth/AuthAppService.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SyZero;
using SyZero.Authorization.Entities;
using SyZero.Domain.Repository;
using SyZero.Runtime.Security;
using SyZero.Runtime.Session;
using MyApp.IApplication.Auth;

namespace MyApp.Application.Auth;

public class AuthAppService : SyZeroServiceBase, IAuthAppService
{
    private readonly UserManager<User> _userManager;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IToken _token;
    private readonly ISySession _session;

    public AuthAppService(
        UserManager<User> userManager,
        IPasswordHasher<User> hasher,
        IToken token,
        ISySession session)
    {
        _userManager = userManager;
        _hasher = hasher;
        _token = token;
        _session = session;
    }

    public async Task<long> RegisterAsync(RegisterDto input)
    {
        var user = new User
        {
            UserName = input.UserName,
            NormalizedUserName = input.UserName.ToUpperInvariant(),
            Email = input.Email,
            NormalizedEmail = input.Email?.ToUpperInvariant(),
            EmailConfirmed = false,
            IsActive = true
        };
        var result = await _userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            throw new SyMessageException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
        return user.Id;
    }

    public async Task<LoginResultDto> LoginAsync(LoginDto input)
    {
        var user = await _userManager.FindByNameAsync(input.UserName)
            ?? throw new SyMessageException("用户不存在");
        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash!, input.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            throw new SyMessageException("密码错误");
        }

        var claims = new[]
        {
            new Claim(SyClaimTypes.UserId, user.Id.ToString()),
            new Claim(SyClaimTypes.UserName, user.UserName!)
        };
        var jwt = _token.CreateAccessToken(claims);
        return new LoginResultDto
        {
            AccessToken = jwt,
            UserId = user.Id,
            UserName = user.UserName!
        };
    }

    public Task<string> WhoAmIAsync()
    {
        // 受保护：未登录直接抛 NoPermission；这里也可加 CheckPermission("MyApp.Profile.Read")
        if (_session.UserId is null)
        {
            throw new SyMessageException(SyMessageBoxStatus.NoPermission);
        }
        return Task.FromResult($"UserId={_session.UserId}, UserName={_session.UserName}");
    }
}
```

### 3) `Web/Program.cs` — 装配

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddSyZero();
builder.Services.AddControllers();
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions { DefaultApiPrefix = "/api" });

// 仓储（任选，Authorization 模块只依赖 IRepository<T>）
builder.Services.AddSyZeroEntityFramework<MyAppDbContext>(opt => opt.UseSqlite("Data Source=app.db"));

// 鉴权
builder.Services.AddSyZeroAuthorization(builder.Configuration, jwt =>
{
    jwt.Issuer = "MyApp";
    jwt.Audience = "MyApp";
    jwt.SecretKey = builder.Configuration["Jwt:SecretKey"]!;
});

// 注册一个权限定义 Provider
builder.Services.AddSingleton<IPermissionDefinitionProvider, MyAppPermissionProvider>();

var app = builder.Build();
app.UseSyZero();
app.UseRouting();
app.UseSyZeroAuthorization();           // = UseAuthentication + UseAuthorization
app.MapControllers();
app.Run();
```

`MyAppDbContext` 必须包含 Authorization 模块全部 7 个实体（参见前文「数据库迁移要点」）。

### 4) 调用示例

```http
POST /api/myapp/auth/register
Content-Type: application/json

{ "userName": "alice", "password": "Abcd1234", "email": "alice@example.com" }
```

```http
POST /api/myapp/auth/login
Content-Type: application/json

{ "userName": "alice", "password": "Abcd1234" }

# 响应：
# { "accessToken": "eyJhbGc...", "userId": 12345, "userName": "alice" }
```

```http
GET /api/myapp/auth/who-am-i
Authorization: Bearer eyJhbGc...
```

> 业务接口要按权限保护时，在 AppService 内调用 `CheckPermission("MyApp.Users.Create")` 或在接口上加 `[CheckPermission("MyApp.Users.Create")]`（来自 SyZero.Core 的属性）。`PermissionSyncHostedService` 会在应用启动时把 `IPermissionDefinitionProvider` 中定义的权限节点同步到 `Permission` / `PermissionGroup` 表，便于后台分配角色。

