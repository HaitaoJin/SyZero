# SyZero.Observability.OpenTelemetry

SyZero 的 OpenTelemetry 扩展，负责注册 ASP.NET Core / HttpClient 的 tracing、metrics 和 logging，并按配置挂接 OTLP exporter。

## 安装

```bash
dotnet add package SyZero.Observability.OpenTelemetry
```

## 快速开始

```json
{
  "SyZero": {
    "Name": "order-service"
  },
  "OpenTelemetry": {
    "OtlpUrl": "http://localhost:4317",
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true,
    "AspNetCorePathPrefixes": [ "/api" ],
    "HttpClientPathPrefixes": [ "/api" ],
    "ActivitySources": [ "OrderService" ],
    "Meters": [ "OrderService" ]
  }
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();
builder.Services.AddSyZeroOpenTelemetry(builder.Configuration);
```

也可以通过代码覆盖配置：

```csharp
builder.Services.AddSyZeroOpenTelemetry(options =>
{
    options.ServiceName = "order-service";
    options.OtlpUrl = "http://localhost:4318";
    options.OtlpProtocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
    options.HttpClientPathPrefixes = Array.Empty<string>(); // 采集所有出站 HTTP 请求
}, builder.Configuration);
```

## 可用重载

```csharp
services.AddSyZeroOpenTelemetry(configuration);
services.AddSyZeroOpenTelemetry(options => { }, configuration);
```

## 配置说明

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `ServiceName` | `string` | `SyZero:Name` 或入口程序集名 | 资源中的服务名 |
| `ServiceVersion` | `string` | 入口程序集版本 | 资源中的服务版本 |
| `OtlpUrl` | `string` | `null` | OTLP 导出地址；Aspire 注入 `OTEL_EXPORTER_OTLP_ENDPOINT` 时自动使用该地址 |
| `OtlpProtocol` | `OtlpExportProtocol` | `Grpc` | OTLP 协议 |
| `EnableTracing` | `bool` | `true` | 是否启用 tracing |
| `EnableMetrics` | `bool` | `true` | 是否启用 metrics |
| `EnableLogging` | `bool` | `true` | 是否启用 logging |
| `ActivitySources` | `string[]` | 空数组 | 需要额外订阅的 `ActivitySource` 名称 |
| `Meters` | `string[]` | 空数组 | 需要额外订阅的 `Meter` 名称；内置会额外监听 `Microsoft.AspNetCore.Hosting`、`Microsoft.AspNetCore.Server.Kestrel`、`Microsoft.AspNetCore.Routing`、`System.Net.Http` |
| `AspNetCorePathPrefixes` | `string[]` | `["/api"]` | 需要采集的入站请求路径前缀 |
| `HttpClientPathPrefixes` | `string[]` | 空数组 | 需要采集的出站请求路径前缀；为空表示不过滤 |

## 说明

- `AddSource("*")` / `AddMeter("*")` 不会自动订阅所有自定义 source/meter，因此这里改成显式配置名称。
- metrics 默认通过监听 .NET 8+ 的内置 meter 名称来采集 HTTP / Kestrel / Routing 指标，而不是依赖当前目标框架下不可用的 metrics instrumentation 扩展。
- 如果没有配置 `OtlpUrl`，模块仍然会完成 OpenTelemetry 注册，但不会主动创建 OTLP 导出连接。
- 标准环境变量 `OTEL_EXPORTER_OTLP_ENDPOINT`、`OTEL_EXPORTER_OTLP_PROTOCOL`、`OTEL_SERVICE_NAME` 会覆盖对应应用配置，可直接接入 Aspire Dashboard 等兼容 OTLP 的运行环境。
- `AspNetCorePathPrefixes` 会同时支持 `/api` 和 `/api/...` 这种匹配，避免只识别带尾斜杠的路径。

## 接入 Aspire Dashboard

通过仓库 AppHost 启动时不需要配置 `OtlpUrl`：

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

Aspire 会为每个项目注入 `OTEL_EXPORTER_OTLP_ENDPOINT`、`OTEL_EXPORTER_OTLP_PROTOCOL` 和资源属性，SyZero 优先读取这些标准配置。Dashboard 固定地址为 `https://localhost:57289`。

若应用不是由 AppHost 启动，则必须选择一种方式指定采集器：

```json
{
  "OpenTelemetry": {
    "OtlpUrl": "http://localhost:4317"
  }
}
```

或者设置环境变量：

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

完整本地验证流程见 [AppHost 本地编排与可观测性](/guide/apphost-observability)。

## 常见问题

### 结构化日志为空

确认 `EnableLogging` 为 `true`，并检查 `Logging:LogLevel`。例如默认级别为 `Warning` 时，普通 `Information` 日志不会进入导出管道。需要格式化消息和作用域时，可以显式注册：

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});
```

### 跟踪为空

默认只采集 `/api` 入站路径。若要采集健康检查或其他入口，可增加前缀；空数组表示不过滤：

```csharp
builder.Services.AddSyZeroOpenTelemetry(options =>
{
    options.AspNetCorePathPrefixes = Array.Empty<string>();
    options.HttpClientPathPrefixes = Array.Empty<string>();
}, builder.Configuration);
```

### 指标页没有可选指标

先向目标服务发送请求并等待一个导出周期。模块默认订阅 ASP.NET Core Hosting、Kestrel、Routing 和 `System.Net.Http` meter；自定义 meter 必须加入 `Meters`。

### Dashboard 中仍显示旧错误

Dashboard 会保留当前运行期已经采集的数据。修复后应观察新的时间戳或重启 AppHost，不要把重启前的红色跟踪当作新的失败。

## 许可证

MIT License - 详见 [LICENSE](../../LICENSE)


