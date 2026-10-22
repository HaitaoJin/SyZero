# SyZero.RateLimiting

Rate limiting module: wraps `System.Threading.RateLimiting` and the ASP.NET Core RateLimiter middleware with sensible defaults (per-IP fixed window) and exposes an `IDistributedRateLimiter` abstraction. Distributed implementation lives in SyZero.Redis.

## 📦 Installation

```bash
dotnet add package SyZero.RateLimiting
```

## ✨ Features

- 🚦 Out-of-the-box global limiter partitioned by client IP
- 🧩 `IDistributedRateLimiter` abstraction — local default + Redis impl from SyZero.Redis
- ⚙️ Configurable window/permits/rejection status

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroRateLimiting(builder.Configuration);

app.UseRouting();
app.UseSyZeroRateLimiting();
app.MapControllers().RequireRateLimiting("syzero-global");

// Custom usage
public class LoginService(IDistributedRateLimiter limiter)
{
    public async Task<bool> TryLoginAsync(string user)
        => await limiter.TryAcquireAsync($"login:{user}", permitLimit: 5, window: 60);
}
```

## ⚙️ Options (`SyZeroRateLimiting`)

| Field | Default |
|---|---|
| `EnableGlobal` | `true` |
| `GlobalPolicyName` | `syzero-global` |
| `GlobalPermitLimit` | `100` |
| `GlobalWindowSeconds` | `60` |
| `RejectionStatusCode` | `429` |

## 📜 License

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
