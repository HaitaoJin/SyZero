# SyZero.Gateway.YARP

YARP implementation of the SyZero gateway abstraction (`SyZero.Gateway` namespace, defined in `SyZero.Gateway.Abstractions`). Built on top of [Yarp.ReverseProxy](https://microsoft.github.io/reverse-proxy/) and shares the very same `configuration.json` schema with `SyZero.Gateway.Ocelot`, so you can swap the underlying gateway without touching the configuration file or business code.

> 📐 An internal `SyZeroYarpProxyConfigProvider` (an `IProxyConfigProvider` implementation) hosts the in-memory route table; whenever the configuration is rewritten the provider raises an `IChangeToken` and YARP rebuilds its routing table without restarting the process.

## 📦 Install

```bash
dotnet add package SyZero.Gateway.YARP
```

## ✨ Highlights

- 🚀 **High-performance reverse proxy** — backed by Microsoft YARP
- 🔄 **Hot reload** — `IGatewayConfigurationStore.WriteAsync` triggers `IChangeToken`-based reconfiguration
- 🔁 **Schema-compatible with Ocelot** — same `Routes` / `UpstreamPathTemplate` / `DownstreamPathTemplate` / `ServiceName` semantics
- ⚖️ **Load-balancing mapping** — `RoundRobin` / `LeastConnection` / `NoLoadBalance` are translated into the corresponding YARP policies

---

## 🚀 Quick start

### 1. `configuration.json` (shared with Ocelot)

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/user-service/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST" ],
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamScheme": "http",
      "ServiceName": "user-service",
      "LoadBalancerOptions": { "Type": "RoundRobin" }
    }
  ]
}
```

### 2. Wire it up

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddSyZero();

builder.Services.AddSyZeroYarp(opt =>
{
    opt.LoadFromConfigurationFile = true;
}, builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();
app.UseSyZero();

app.UseRouting();
app.UseCors("SyZero.Gateway.YARP");
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapSyZeroYarp();
});
app.Run();
```

---

## 📖 Options

| Property | Type | Default | Notes |
|---|---|---|---|
| `CorsPolicyName` | `string` | `"SyZero.Gateway.YARP"` | Named CORS policy |
| `ConfigurationFileName` | `string` | `"configuration.json"` | File under content-root |
| `LoadFromConfigurationFile` | `bool` | `true` | Whether to seed YARP from the file at startup |
| `LoadFromReverseProxySection` | `bool` | `false` | Also load the native `ReverseProxy` section from `appsettings.json` |
| `AllowedOrigins` | `string[]` | derived from `SyZero:CorsOrigins` | CORS allow-list |
| `EnableSwagger` / `EnableSwaggerGen` | `bool` | `false` | YARP has no built-in Swagger aggregator |

---

## 🔁 SyZero.Gateway.Ocelot vs SyZero.Gateway.YARP

| Capability | SyZero.Gateway.Ocelot | SyZero.Gateway.YARP |
|---|---|---|
| Routing & load-balancing | ✅ | ✅ |
| Consul service discovery | ✅ built-in | ⚠️ needs an external `IDestinationResolver` |
| Polly circuit-breaker | ✅ | ⚠️ via YARP `HttpClient` config |
| Swagger aggregation | ✅ MMLib.SwaggerForOcelot | ❌ |
| Performance | medium | high |
| Hot reload | ✅ | ✅ |
| Compatible with SyZero.Gateway.Admin | ✅ | ✅ (shared `IGatewayConfigurationStore`) |

---

## 🌐 Service discovery (Consul / Nacos / Redis / Local / DB)

`SyZero.Gateway.YARP` ships with an `IDestinationResolver` built on top of SyZero's
`IServiceManagement` abstraction, so wiring up any SyZero discovery backend
into YARP is one extra line.

```csharp
builder.Services.AddSyZeroYarp(builder.Configuration);
builder.Services.AddConsul(builder.Configuration); // or AddNacos / AddRedisServiceManagement / AddLocal/DBServiceManagement
builder.Services.AddSyZeroYarpServiceDiscovery();  // ★ register the resolver
```

How it works:

1. `YarpConfigMapper` emits a placeholder destination `discovery://{ServiceName}/` for every cluster whose route specifies a `ServiceName`.
2. When YARP loads/refreshes the cluster, the resolver detects the `discovery://` scheme and calls `IServiceManagement.GetHealthyServices(serviceName)` to expand the placeholder into real `{scheme}://{host}:{port}` destinations (gRPC is exposed via H2 over HTTP).
3. Destinations with any other scheme are passed through unchanged so you can mix static and dynamic clusters.

Instance churn is reflected automatically: on first resolve the resolver subscribes via `IServiceManagement.Subscribe(...)` and emits an `IChangeToken`. When Consul/Nacos/Redis push a change, YARP refreshes **only that cluster's destinations** without rereading the gateway configuration file. The resolver implements `IDisposable` and unsubscribes cleanly on shutdown.

---

## ⚖️ Load balancing & health checks

`GatewayLoadBalancerOptions.Type` (neutral schema, Ocelot-style names) is translated to YARP's native policy by `YarpConfigMapper`, so the same `configuration.json` behaves consistently on both backends:

| `GatewayLoadBalancerOptions.Type` | YARP `LoadBalancingPolicy` | Ocelot `LoadBalancerOptions.Type` |
|---|---|---|
| `RoundRobin` (default) | `RoundRobin` | `RoundRobin` |
| `LeastConnection` | `LeastRequests` | `LeastConnection` |
| `NoLoadBalance` | `FirstAlphabetical` | `NoLoadBalancer` |
| other / null | YARP default (`PowerOfTwoChoices`) | Ocelot default |

For health checks SyZero relies on the **service-discovery source of truth**: `IServiceManagement.GetHealthyServices(...)` only returns instances already deemed healthy by Consul/Nacos/Redis/etc. Therefore YARP's active health probe is **not enabled by default**, avoiding double-judging health. Instance churn is pushed via `Subscribe(...)` (see above). If you still want YARP active probing, set `LoadFromReverseProxySection = true` and configure `ReverseProxy:Clusters:<id>:HealthCheck` in `appsettings.json`.

---

## ⚠️ Caveats

1. The neutral schema only carries `ServiceName`; clusters are aggregated by service name. Either enable the [service-discovery integration](#-service-discovery-consul--nacos--redis--local--db) above to let `IServiceManagement` resolve real instances, or set `LoadFromReverseProxySection = true` and define `Clusters[*].Destinations` under `ReverseProxy` in `appsettings.json`.
2. Ocelot-style `{everything}` is automatically rewritten to YARP `{**catchAll}`; single-segment placeholders (`{id}`) work as-is.
3. `System.Text.Json` does not preserve `//` comments on write; the timestamped backup keeps the original content.

---

## 📄 License

MIT License — see [LICENSE](../../../LICENSE).
