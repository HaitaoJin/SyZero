# SyZero.Authorization

Authentication / authorization module for SyZero. One-stop wiring of **ASP.NET Core Identity (with `IUserStore` / `IRoleStore` backed by SyZero `IRepository<T>`) + ABP-style RBAC (PermissionGroup / Permission / Role / UserRole / RolePermission / UserPermission) + JWT Bearer**.

## Design overview

| Capability | Implementation |
|---|---|
| User / role | `User` / `Role` entities + `SyZeroUserStore` / `SyZeroRoleStore` (implement `IUserStore` / `IRoleStore`, persisted via `IRepository<T>`) |
| Permission definitions | Code-first `IPermissionDefinitionProvider` + `PermissionGroupDefinition` / `PermissionDefinition` (tree shape) |
| Permission storage | `PermissionGroup` / `Permission` entities; `PermissionSyncHostedService` upserts code-defined entries on startup |
| Permission grants | Role-level `RolePermission` + user-level `UserPermission` (user-level overrides role-level; any explicit deny short-circuits) |
| Permission check | `RbacPermissionChecker` replaces `SyZero.Core`'s `DefaultPermissionChecker` (transparent); business code keeps `[CheckPermission]` / `IPermissionChecker.Authorize(...)` |
| Tokens | `JwtTokenManager` implements `IToken` (HMAC-SHA256), configured via `JwtOptions`; ASP.NET Core JwtBearer is wired in the same call |
| Refresh tokens | `IRefreshTokenManager` + `RefreshTokenManager`: 32-byte random token, **only the SHA-256 hash is persisted**, replay detection cascades into `RevokeAllAsync` |
| Account convenience | `SyZeroAccountManager` glues `UserManager<User>` + `IRefreshTokenManager` (`LogoutAsync` / `LogoutAllAsync` / `ChangePasswordAsync` / `ResetPasswordAsync`); password change auto-revokes all tokens |

## Dependencies

- `SyZero.Core`
- `Microsoft.AspNetCore.App` (`FrameworkReference`, supplies Identity managers / JwtBearer)
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`

You also need a SyZero repository provider (e.g. `SyZero.EntityFrameworkCore` / `SyZero.SqlSugar`) to register `IRepository<>`.

## Quick start

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

Your `DbContext` must include `DbSet<User>`, `DbSet<Role>`, `DbSet<UserRole>`, `DbSet<RolePermission>`, `DbSet<UserPermission>`, `DbSet<PermissionGroup>`, `DbSet<Permission>`, and (if you use refresh tokens) `DbSet<RefreshToken>`.

## Defining permissions

```csharp
public class MyAppPermissionProvider : IPermissionDefinitionProvider
{
    public void Define(PermissionDefinitionContext ctx)
    {
        var g = ctx.AddGroup("MyApp", "My application");
        var users = g.AddPermission("MyApp.Users", "User management");
        users.AddChild("MyApp.Users.Create", "Create user");
        users.AddChild("MyApp.Users.Update", "Update user");
        users.AddChild("MyApp.Users.Delete", "Delete user");
    }
}

// Register manually — auto-DI does not pick up providers
builder.Services.AddSingleton<IPermissionDefinitionProvider, MyAppPermissionProvider>();
```

`PermissionSyncHostedService` will upsert these into the `PermissionGroup` / `Permission` tables on startup.

## Checking permissions

```csharp
public class UserAppService : SyZeroServiceBase, IUserAppService
{
    public async Task CreateAsync(CreateUserDto input)
    {
        CheckPermission("MyApp.Users.Create"); // throws SyMessageException(NoPermission) when denied
        // ...
    }
}
```

## JWT configuration

`appsettings.json`:

```json
{
  "Jwt": {
    "Issuer": "MyApp",
    "Audience": "MyApp",
    "SecretKey": "replace-with-a-32+-char-random-string",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```

Issuing an access token:

```csharp
var claims = new[]
{
    new Claim(SyClaimTypes.UserId, user.Id.ToString()),
    new Claim(SyClaimTypes.UserName, user.UserName)
};
var accessToken = tokenManager.CreateAccessToken(claims); // tokenManager: IToken
```

## Refresh tokens

`IRefreshTokenManager` issues, rotates and revokes refresh tokens. Only the **SHA-256 hash** of the raw token (Base64Url) is stored; the raw value is returned to the caller exactly once.

| Method | Behaviour |
| --- | --- |
| `IssueAsync(userId, claims, ip?, ct)` | Issues an access + refresh token pair; persists the hash with `ExpiresAt = now + RefreshTokenExpirationDays`. |
| `RotateAsync(refreshToken, newClaims, ip?, ct)` | Looks up the hash. If already revoked, **cascades** `RevokeAllAsync` (replay detection) and throws `SyMessageException`. If valid, mints a new pair and marks the old token as revoked + replaced. |
| `RevokeAsync(refreshToken, ct)` | Revokes a single token (single-device logout). |
| `RevokeAllAsync(userId, ct)` | Revokes every active refresh token for a user (log out everywhere). |

Recommended deployment: store the refresh token in an **HttpOnly + Secure + SameSite=Strict cookie**; let the front-end keep only the short-lived access token. Treat any "revoked" `SyMessageException` from `RotateAsync` as a suspected leak and force a re-login.

## Account convenience (`SyZeroAccountManager`)

Auto-registered (`IScopedDependency`) thin glue over `UserManager<User>` + `IRefreshTokenManager`. Use it whenever an operation must invalidate refresh tokens:

| Method | Behaviour |
| --- | --- |
| `LogoutAsync(refreshToken, ct)` | Revokes the current device's refresh token. |
| `LogoutAllAsync(userId, ct)` | Revokes every refresh token for the user. |
| `ChangePasswordAsync(userId, current, new, ct)` | `UserManager.ChangePasswordAsync`; **on success** auto-`RevokeAllAsync`. |
| `ResetPasswordAsync(userId, new, ct)` | Admin-style reset (generates a reset token internally); **on success** auto-`RevokeAllAsync`. |

> Old access tokens remain valid until natural expiry; consider shortening `AccessTokenExpirationMinutes`.

## Database migration hints (EF Core)

```csharp
modelBuilder.Entity<User>().HasIndex(u => u.NormalizedUserName).IsUnique();
modelBuilder.Entity<Role>().HasIndex(r => r.NormalizedName).IsUnique();
modelBuilder.Entity<UserRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.PermissionName }).IsUnique();
modelBuilder.Entity<UserPermission>().HasIndex(x => new { x.UserId, x.PermissionName }).IsUnique();
modelBuilder.Entity<Permission>().HasIndex(p => p.Name).IsUnique();
modelBuilder.Entity<PermissionGroup>().HasIndex(g => g.Name).IsUnique();
modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
modelBuilder.Entity<RefreshToken>().HasIndex(x => x.UserId);
```

## Compatibility notes

- `IPermissionChecker.IsHas(name)` is **synchronous** by design (the check stays in-process and uses cached lookups).
- `ISySession.Principal` is read-only — populate it through the JwtBearer middleware, not by direct assignment.
- `SyZero.Core` targets `netstandard2.1`; this module targets `net9.0`.

For the full Chinese reference (including a four-layer `Web / Application / IApplication / Core` registration + login + protected endpoint sample), see [the Chinese version](/modules/SyZero.Authorization).
