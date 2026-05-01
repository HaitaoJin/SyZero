# SyZero.Diagnostics

A unified request diagnostics middleware that logs structured fields (method, path, status, elapsed, TraceId, UserId, TenantId) and writes them into `HttpContext.Items` for downstream code.

## 📦 Install

```bash
dotnet add package SyZero.Diagnostics
```

## 🚀 Quick start

```csharp
app.UseSyZeroDiagnostics();   // place as early as possible
app.UseAuthentication();
app.UseAuthorization();
```

Keys written into `HttpContext.Items`:

| Key | Source |
| --- | --- |
| `sy.TraceId` | `Activity.Current?.TraceId` or `HttpContext.TraceIdentifier` |
| `sy.UserId` | `HttpContext.User.Identity.Name` |
| `sy.TenantId` | `X-Tenant-Id` header |

When [SyZero.OpenTelemetry](./SyZero.OpenTelemetry) is enabled, ASP.NET Core auto-populates `Activity.Current`, so the middleware records the correct TraceId without further configuration.
