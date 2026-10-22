# SyZero.HealthChecks

Health checks module: builds on top of `Microsoft.Extensions.Diagnostics.HealthChecks` and exposes SyZero-style registration with three Kubernetes-friendly endpoints — `/health/live`, `/health/ready`, `/health`.

## 📦 Installation

```bash
dotnet add package SyZero.HealthChecks
```

## ✨ Features

- 🟢 **Three endpoints**: `live` (process only) / `ready` (dependencies) / `/health` (all)
- 🏷️ **Tag-based filtering** via `LiveTag` / `ReadyTag`
- 📦 **JSON detailed response** with per-check duration and exception
- 🔧 **Configurable paths** via `appsettings.json` or code

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { SyZeroHealthChecksExtension.LiveTag })
    .AddCheck<SqlServerHealthCheck>("sqlserver", tags: new[] { SyZeroHealthChecksExtension.ReadyTag });

app.UseRouting();
app.UseEndpoints(endpoints => endpoints.UseSyZeroHealthChecks());
```

## ⚙️ Options (`SyZeroHealthChecks` section)

| Field | Default | Description |
|---|---|---|
| `LivePath` | `/health/live` | Liveness probe path |
| `ReadyPath` | `/health/ready` | Readiness probe path |
| `OverallPath` | `/health` | Aggregate endpoint |
| `DetailedResponse` | `true` | Output JSON with per-check details |

## 📜 License

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
