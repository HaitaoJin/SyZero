# SyZero.RateLimiting

限流模块：包装 .NET 内置 `System.Threading.RateLimiting` 与 ASP.NET Core 限流中间件，提供按 IP 的全局固定窗口策略；同时给出 `IDistributedRateLimiter` 抽象，可由 SyZero.Redis 提供分布式实现。

## 📦 安装

```bash
dotnet add package SyZero.RateLimiting
```

## ✨ 特性

- 🚦 **开箱即用全局限流**：默认按客户端 IP 分区、固定窗口
- 🧩 **`IDistributedRateLimiter` 抽象**：进程内默认实现 `LocalDistributedRateLimiter`，分布式实现见 SyZero.Redis
- ⚙️ **可配置**：窗口长度、令牌数、拒绝状态码均可在 `appsettings.json` 调整

## 🚀 快速开始

### 1. 注册

```csharp
builder.Services.AddSyZeroRateLimiting(builder.Configuration);
```

### 2. 中间件

```csharp
app.UseRouting();
app.UseSyZeroRateLimiting();
app.MapControllers().RequireRateLimiting("syzero-global");
```

### 3. 业务侧使用抽象

```csharp
public class LoginService
{
    private readonly IDistributedRateLimiter _limiter;
    public LoginService(IDistributedRateLimiter limiter) => _limiter = limiter;

    public async Task<bool> TryLoginAsync(string userName)
    {
        if (!await _limiter.TryAcquireAsync($"login:{userName}", permitLimit: 5, window: 60))
        {
            throw new BusinessException("登录过于频繁，请稍后再试");
        }
        // ... 业务 ...
        return true;
    }
}
```

## ⚙️ 配置项

`appsettings.json` 中的 `SyZeroRateLimiting` 节：

| 字段 | 默认值 | 说明 |
|---|---|---|
| `EnableGlobal` | `true` | 是否启用全局策略 |
| `GlobalPolicyName` | `syzero-global` | 策略名称 |
| `GlobalPermitLimit` | `100` | 窗口允许请求数 |
| `GlobalWindowSeconds` | `60` | 窗口长度（秒） |
| `RejectionStatusCode` | `429` | 拒绝时的 HTTP 状态码 |

## 📜 许可证

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
