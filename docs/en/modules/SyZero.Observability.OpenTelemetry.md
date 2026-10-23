# SyZero.Observability.OpenTelemetry

SyZero.Observability.OpenTelemetry adds tracing, metrics, and logging integration with OTLP export for observability in distributed systems.

## Install

```bash
dotnet add package SyZero.Observability.OpenTelemetry
```

## Key Features

- Distributed tracing
- Metrics collection
- Logging pipeline integration
- OTLP export support
- Path-based instrumentation filtering

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();
builder.Services.AddSyZeroOpenTelemetry(builder.Configuration);
```

Code can override configuration:

```csharp
builder.Services.AddSyZeroOpenTelemetry(options =>
{
    options.ServiceName = "order-service";
    options.OtlpUrl = "http://localhost:4318";
    options.OtlpProtocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
    options.HttpClientPathPrefixes = Array.Empty<string>();
}, builder.Configuration);
```

## Configuration Example (appsettings.json)

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

## Configuration

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `ServiceName` | `string` | `SyZero:Name` or entry assembly | Service name attached to resources |
| `ServiceVersion` | `string` | Entry assembly version | Service version attached to resources |
| `OtlpUrl` | `string` | `null` | OTLP endpoint; Aspire-injected `OTEL_EXPORTER_OTLP_ENDPOINT` takes precedence |
| `OtlpProtocol` | `OtlpExportProtocol` | `Grpc` | OTLP transport protocol |
| `EnableTracing` | `bool` | `true` | Enables tracing |
| `EnableMetrics` | `bool` | `true` | Enables metrics |
| `EnableLogging` | `bool` | `true` | Enables logging export |
| `ActivitySources` | `string[]` | Empty | Additional `ActivitySource` names to subscribe to |
| `Meters` | `string[]` | Empty | Additional meter names; built-ins cover Hosting, Kestrel, Routing, and `System.Net.Http` |
| `AspNetCorePathPrefixes` | `string[]` | `["/api"]` | Inbound path prefixes to capture |
| `HttpClientPathPrefixes` | `string[]` | Empty | Outbound path prefixes; empty means no filtering |

Standard `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL`, and `OTEL_SERVICE_NAME` values override application configuration. `grpc`, `http/protobuf`, and `httpprotobuf` protocol values are recognized.

## Aspire Dashboard

No `OtlpUrl` is required when the repository AppHost starts the application:

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

Aspire injects the standard OTLP settings into every project, and SyZero consumes them automatically. The Dashboard is fixed at `https://localhost:57289`.

For a standalone launch, configure a collector either in `appsettings.json` or through environment variables:

```text
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

See [AppHost Local Orchestration and Observability](/en/guide/apphost-observability) for the complete local workflow.

## Troubleshooting

### Structured logs are empty

Confirm that `EnableLogging` is `true` and inspect `Logging:LogLevel`. A `Warning` minimum level filters ordinary `Information` messages. To include formatted messages and scopes explicitly:

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});
```

### Traces are empty

Inbound tracing captures `/api` by default. Add other path prefixes or use empty arrays to disable path filtering:

```csharp
builder.Services.AddSyZeroOpenTelemetry(options =>
{
    options.AspNetCorePathPrefixes = Array.Empty<string>();
    options.HttpClientPathPrefixes = Array.Empty<string>();
}, builder.Configuration);
```

### No metrics are available

Send requests to the selected service and wait for an export interval. The module subscribes to ASP.NET Core Hosting, Kestrel, Routing, and `System.Net.Http` meters by default. Add custom meters through `Meters`.

### The Dashboard still shows an old error

The Dashboard retains telemetry collected during the current run. After a fix, inspect new timestamps or restart AppHost; red traces from before the restart are historical records.

## References

- Chinese documentation: [/modules/SyZero.Observability.OpenTelemetry](/modules/SyZero.Observability.OpenTelemetry)



