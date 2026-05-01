# SyZero.Authentication.ApiKey

最小化 API Key 认证 Scheme，按请求头映射 ApiKey → 主体名。

## 📦 安装

```bash
dotnet add package SyZero.Authentication.ApiKey
```

## 🚀 快速开始

```csharp
builder.Services
    .AddAuthentication(ApiKeyOptions.Scheme)
    .AddSyZeroApiKey(o =>
    {
        o.HeaderName = "X-Api-Key";   // 默认值
        o.Keys["sk_live_xxx"] = "service-account-1";
        o.Keys["sk_live_yyy"] = "service-account-2";
    });

app.UseAuthentication();
app.UseAuthorization();
```

## 🔐 行为

| 场景 | 结果 |
| --- | --- |
| 缺失 / 空白请求头 | `AuthenticateResult.NoResult()` |
| 请求头存在但 key 不在白名单 | `AuthenticateResult.Fail("无效的 API Key")` |
| key 命中 | 成功，`ClaimsIdentity.Name = Keys[key]` |

## ⚠️ 注意

- 适合服务间或受控集成；面向终端用户请使用 JWT / OIDC。
- 推荐结合 `SyZero.RateLimiting` + `SyZero.Audit` 做请求节流与审计。
