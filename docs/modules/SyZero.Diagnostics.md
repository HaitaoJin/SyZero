# SyZero.Diagnostics

统一的请求诊断中间件：写入 method / path / status / 耗时 / TraceId / UserId / TenantId 结构化日志，并把这些字段填入 `HttpContext.Items` 便于业务读取。

## 📦 安装

```bash
dotnet add package SyZero.Diagnostics
```

## 🚀 快速开始

```csharp
app.UseSyZeroDiagnostics();   // 建议尽量靠前
app.UseAuthentication();
app.UseAuthorization();
```

## 📌 写入 HttpContext.Items 的键

| 键 | 含义 |
| --- | --- |
| `sy.TraceId` | `Activity.Current?.TraceId` 或 `HttpContext.TraceIdentifier` |
| `sy.UserId` | `HttpContext.User.Identity.Name`（如已认证） |
| `sy.TenantId` | 请求头 `X-Tenant-Id` |

## 🔗 与 OpenTelemetry 配合

启用 [SyZero.Observability.OpenTelemetry](./SyZero.Observability.OpenTelemetry) 后，`Activity.Current` 由 ASP.NET Core 自动填充，本中间件无需额外配置即可写入正确的 TraceId。

