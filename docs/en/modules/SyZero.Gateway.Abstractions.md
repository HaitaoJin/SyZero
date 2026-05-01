# SyZero.Gateway.Abstractions

> Introduced in v1.6.0. Previously the same abstractions (namespace `SyZero.Gateway`) lived inside `SyZero.Core`; they have been extracted into their own package so that `SyZero.Core` truly focuses on the framework kernel (DI / repository / service management / event bus / multi-level cache).

`SyZero.Gateway.Abstractions` contains **interfaces and DTOs only** — no specific gateway dependency. All SyZero gateway implementations (`SyZero.Gateway.Ocelot`, `SyZero.Gateway.YARP`) and the admin panel (`SyZero.Gateway.Admin`) share a single set of abstractions through this package, so implementations are pluggable, the admin panel is reusable, and the configuration schema is identical.

## Installation

```bash
dotnet add package SyZero.Gateway.Abstractions
```

> You typically do **not** reference this package directly — reference a concrete implementation (`SyZero.Gateway.Ocelot` or `SyZero.Gateway.YARP`), which already pulls `SyZero.Gateway.Abstractions` transitively. Reference it directly only when you author a new `IGateway` backend.

## Namespace

The whole abstraction layer lives under `SyZero.Gateway` (the namespace differs from the package id deliberately, so existing `using SyZero.Gateway;` keeps working).

```csharp
using SyZero.Gateway;
```

## Core abstractions

| Abstraction | Purpose | Typical implementations |
|---|---|---|
| `IGateway` | Currently running gateway runtime (`ProviderName` + `GetConfigurationAsync`) | `OcelotGateway`, `YarpGateway` |
| `IGatewayConfigurationStore` | Gateway configuration persistence (read / write / reload / backup lifecycle) | `FileGatewayConfigurationStore` (in `SyZero.Gateway.Admin`), `OcelotConfigurationStore`, `YarpConfigurationStore` |
| `IGatewayConfigurationApplier` | Hot-applies `GatewayConfiguration` to the running gateway pipeline | `OcelotConfigurationApplier`, `YarpConfigurationApplier` |
| `GatewayConfiguration` | Top-level configuration DTO (`Routes` + `SwaggerEndPoints` + `Extra`, with a global rate-limit bridge) | — |
| `GatewayRoute` | Single route (with `Disabled` / `Tags` / rate-limit / load-balancer) | — |
| `GatewaySwaggerEndPoint` | Aggregated Swagger endpoint | — |
| `GatewayOptions` | Base options class (CORS / Swagger title / Service section reader) | Extended by `SyZeroOcelotOptions`, `SyZeroYarpOptions` |
| `GatewayLoadBalancerOptions` / `GatewayRateLimitOptions` / `GatewayGlobalRateLimitOptions` | DTOs aligned with `ocelot.json`, allowing both backends to share the very same `configuration.json` | — |

## Design notes

- **Schema-compatible with `ocelot.json`**: all DTO fields use PascalCase; `SyZero.Gateway.Ocelot` and `SyZero.Gateway.YARP` share the same `configuration.json` so you can switch implementations without touching configuration.
- **`Extra` pass-through**: `GatewayConfiguration` keeps unknown root-level fields (e.g. `GlobalConfiguration`) via `[JsonExtensionData]` so they survive a round-trip read/write.
- **Optional backup capability**: `IGatewayConfigurationStore` exposes `ListBackupsAsync` / `ReadBackupAsync` / `RestoreBackupAsync` / `DeleteBackupAsync` as default interface methods (which throw `NotSupportedException` unless implemented; `FileGatewayConfigurationStore` in `SyZero.Gateway.Admin` provides a full implementation).

## Authoring a new backend

```csharp
public sealed class MyCustomGateway : IGateway
{
    public string ProviderName => "MyCustom";

    public Task<GatewayConfiguration> GetConfigurationAsync(CancellationToken ct = default)
    {
        // Convert your runtime route table into GatewayConfiguration and return it.
        return Task.FromResult(new GatewayConfiguration());
    }
}
```

Implement `IGatewayConfigurationStore` + `IGatewayConfigurationApplier`, then register in your extension method:

```csharp
services.AddSingleton<IGateway, MyCustomGateway>();
services.AddSingleton<IGatewayConfigurationStore, MyCustomConfigStore>();
services.AddSingleton<IGatewayConfigurationApplier, MyCustomApplier>();
```

`SyZero.Gateway.Admin` will manage your custom gateway out-of-the-box without any extra adapter.

## History

- v1.6.0: extracted from `SyZero.Core` into its own NuGet package. The `SyZero.Gateway` namespace is preserved — business `using` directives need no change; only project references that previously relied on `SyZero.Core` for these types must now reference a concrete implementation package (or this abstractions package).
