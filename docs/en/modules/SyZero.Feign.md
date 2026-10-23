# SyZero.Feign

SyZero.Feign is a declarative HTTP client module based on Refit. Since `v9.1.0` it is implemented as a concrete `IRemoteServiceProvider` defined in `SyZero.Core`, so the same `RemoteServiceOptions` configuration can be reused by future remote-call providers.

## Install

```bash
dotnet add package SyZero.Feign
```

## Key Features

- Declarative client interfaces (Refit)
- HTTP and gRPC over a single options model
- Service discovery and load balancing via `IServiceManagement`
- Pluggable per-protocol factories (`IRemoteServiceProxyFactory`) — register your own with `FeignRemoteServiceProvider.RegisterProxyFactory(...)`

## Quick Start

```csharp
// Register from "Feign" section in appsettings.json
builder.Services.AddSyZeroFeign(builder.Configuration);

// Or pass options inline
builder.Services.AddSyZeroFeign(new RemoteServiceOptions
{
    Service =
    {
        new RemoteServiceItem
        {
            ServiceName = "user-service",
            AssemblyName = "User.IApplication",
            Protocol = RemoteServiceProtocol.Http
        }
    }
});

// Equivalent low-level call
// builder.Services.AddRemoteService<FeignRemoteServiceProvider>();
```

## Configuration Example (appsettings.json)

```json
{
    "Feign": {
        "Service": [
            {
                "ServiceName": "user-service",
                "AssemblyName": "User.IApplication",
                "Protocol": "Http",
                "Timeout": 30,
                "Retry": 3
            }
        ],
        "Global": {
            "Protocol": "Http",
            "Timeout": 30,
            "Retry": 3,
            "EnableSsl": false
        }
    }
}
```

> ⚠️ **Breaking change included in the consolidated `v9.1.0` release**: `FeignOptions` / `FeignService` / `FeignProtocol` / `IFeignProxyFactory` were moved into `SyZero.Core` and renamed to `RemoteServiceOptions` / `RemoteServiceItem` / `RemoteServiceProtocol` / `IRemoteServiceProxyFactory`. The service item field `DllName` was renamed to `AssemblyName`. The `AddSyZeroFeign(...)` extension shape is preserved (only the parameter type changes).

## References

- Chinese documentation: [/modules/SyZero.Feign](/modules/SyZero.Feign)


