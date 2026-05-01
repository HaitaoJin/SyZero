# SyZero.HealthChecks

健康检查模块：在 ASP.NET Core 标准 `Microsoft.Extensions.Diagnostics.HealthChecks` 之上提供 SyZero 风格的注册与端点映射，支持 `/health/live`、`/health/ready`、`/health` 三档端点（K8s 友好）。

## 📦 安装

```bash
dotnet add package SyZero.HealthChecks
```

## ✨ 特性

- 🟢 **三段式端点**：`live`（仅进程存活）/ `ready`（依赖就绪）/ `/health`（全部）
- 🏷️ **标签筛选**：通过 `LiveTag` / `ReadyTag` 区分检查器用途
- 📦 **JSON 详情响应**：默认输出每个检查器的状态、耗时、异常
- 🔧 **可配置路径**：通过 `appsettings.json` 或代码 lambda 自定义

## 🚀 快速开始

### 1. 注册

```csharp
builder.Services.AddSyZeroHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { SyZeroHealthChecksExtension.LiveTag })
    .AddCheck<SqlServerHealthCheck>("sqlserver", tags: new[] { SyZeroHealthChecksExtension.ReadyTag });
```

### 2. 映射端点

```csharp
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.UseSyZeroHealthChecks();
});
```

### 3. K8s 探针配置示例

```yaml
livenessProbe:
  httpGet: { path: /health/live, port: 80 }
readinessProbe:
  httpGet: { path: /health/ready, port: 80 }
```

## ⚙️ 配置项

`appsettings.json` 中的 `SyZeroHealthChecks` 节：

| 字段 | 默认值 | 说明 |
|---|---|---|
| `LivePath` | `/health/live` | 存活探针路径 |
| `ReadyPath` | `/health/ready` | 就绪探针路径 |
| `OverallPath` | `/health` | 全量端点路径 |
| `DetailedResponse` | `true` | 是否输出 JSON 详情 |

## 📜 许可证

[Apache 2.0](https://github.com/SyZeroOrg/SyZero/blob/main/LICENSE)
