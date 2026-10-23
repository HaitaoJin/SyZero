# Architecture

SyZero is a lightweight, modular .NET microservice framework. It keeps core abstractions in `SyZero.Core` and ships concrete capabilities as independent NuGet modules.

## Goals

1. **Composable by need**: reference only the modules a service actually uses
2. **Stable abstractions**: repositories, event bus, service governance, and remote calls are interface-first
3. **Replaceable implementations**: the same capability can use local, database, Redis, Consul, Nacos, RabbitMQ, Kafka, or other backends
4. **Clear business layering**: contracts, application orchestration, domain models, and host infrastructure stay separated
5. **Lightweight development**: start with built-in implementations, then move to production-grade middleware modules when needed

## Package and module boundaries

SyZero modules generally fall into two groups:

| Type | Location | Description |
| --- | --- | --- |
| Core abstractions | `SyZero.Core` | DI, application services, repository/unit-of-work abstractions, service management abstraction, event bus abstraction, and lightweight built-ins |
| Pluggable implementations | `SyZero.Module/SyZero.*` | ASP.NET Core, DynamicWebApi, EF Core, SqlSugar, MongoDB, Redis, Consul, Nacos, RabbitMQ, Kafka, gateway, observability, and more |

New services usually start with:

```bash
dotnet add package SyZero.Core
dotnet add package SyZero.AspNetCore
```

Then add modules by scenario:

```bash
dotnet add package SyZero.DynamicWebApi
dotnet add package SyZero.Swagger
dotnet add package SyZero.SqlSugar
dotnet add package SyZero.Redis
```

## Recommended layers

A business service should usually use four layers:

```text
MyCompany.OrderService/
├─ MyCompany.Order.Web/           # ASP.NET Core host, controllers, middleware, Swagger
├─ MyCompany.Order.Application/   # application service implementations and use-case orchestration
├─ MyCompany.Order.IApplication/  # application service interfaces, DTOs, remote contracts
└─ MyCompany.Order.Core/          # entities, value objects, domain services, repository extensions
```

Recommended dependency direction:

1. `Web` references `Application` and `IApplication`
2. `Application` references `IApplication` and `Core`
3. `IApplication` contains contracts and DTOs, and does not reference `Web`
4. `Core` contains domain models and base business objects, and does not depend on upper layers

This keeps Dynamic Web API, Feign, gRPC, and SDK generation centered around `IApplication`, while database and middleware choices stay out of the contract layer.

## Startup flow

A typical SyZero Web service starts like this:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

builder.Services.AddControllers();
builder.Services.AddDynamicWebApi();
builder.Services.AddSyZeroOpenApi();

var app = builder.Build();

app.UseSyZero();
app.MapControllers();
app.MapSyZeroOpenApi();

app.Run();
```

Key steps:

| Step | Purpose |
| --- | --- |
| `builder.AddSyZero()` | Binds global configuration and registers core services, lifetime marker types, validation, cache, and `SyZeroBootstrapper` |
| `builder.Services.AddSyZero<Module>()` | Registers concrete module capabilities such as repositories, discovery, logging, and event bus |
| `app.UseSyZero()` | Sets the runtime service provider so infrastructure such as `SyZeroServiceBase` can resolve scoped services |
| `SyZeroBootstrapper` | Runs as an `IHostedService` to register/unregister the service during host start/stop |

## Dependency injection conventions

SyZero uses marker interfaces for automatic DI registration:

| Interface | Lifetime | Common use |
| --- | --- | --- |
| `IScopedDependency` | Scoped | Application services, repository wrappers, request-scoped business components |
| `ITransientDependency` | Transient | Stateless lightweight components |
| `ISingletonDependency` | Singleton | Global services, cache coordinators, configuration services |

Example:

```csharp
public interface IUserProfileService
{
    Task<UserProfileDto> GetAsync(long userId);
}

public class UserProfileService : IUserProfileService, IScopedDependency
{
    public Task<UserProfileDto> GetAsync(long userId)
    {
        // ...
    }
}
```

The framework scans non-abstract classes implementing these interfaces and registers them against their implemented interfaces. Prefer constructor injection in business code.

## Application service conventions

Application services are the entry point for use cases:

```csharp
using SyZero.Application.Attributes;
using SyZero.Application.Service;

[DynamicApi]
public interface IOrderAppService : IApplicationService, IDynamicApi
{
    Task<OrderDto> GetAsync(long id);
    Task<long> CreateAsync(CreateOrderInput input);
}

public class OrderAppService : SyZeroServiceBase, IOrderAppService, IScopedDependency
{
    public Task<OrderDto> GetAsync(long id)
    {
        // Query and return DTO
    }

    public Task<long> CreateAsync(CreateOrderInput input)
    {
        // Orchestrate the use case
    }
}
```

Recommended rules:

1. Put public contracts in `IApplication`; service interfaces inherit `IApplicationService`
2. Contracts that should be dynamically exposed inherit `IDynamicApi` and use `[DynamicApi]`
3. Keep DTOs separate from database entities
4. Application services orchestrate use cases; complex domain rules should live in domain services or entities
5. Implementations can inherit `SyZeroServiceBase` to use `ObjectMapper`, `UnitOfWork`, `Logger`, and related base capabilities

## Dynamic API conventions

`SyZero.DynamicWebApi` and `SyZero.DynamicGrpc` share the core dynamic API filtering rules. A dynamically exposed type usually needs to:

1. Implement `IDynamicApi`
2. Have `[DynamicApi]` on the type or an implemented interface
3. Be public, non-abstract, and non-generic
4. Not be excluded by `[NonDynamicApi]`, `[NonWebApiService]`, or related attributes

Web API routes are built from:

```text
/{DefaultApiPrefix}/{DefaultAreaName}/{ControllerName}/{ActionName}
```

Example:

```csharp
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = "OrderService"
});
```

`OrderAppService.GetAsync` removes common postfixes such as `AppService`, `Service`, and `Async`, then generates lowercase routes by default. Use `[Get]`, `[Post]`, `[Put]`, or `[Delete]` on interface methods when you need explicit HTTP methods or paths.

## Configuration conventions

Base service configuration lives under `SyZero`:

```json
{
  "SyZero": {
    "Name": "OrderService",
    "Protocol": "HTTP",
    "Port": 5001,
    "Ip": "",
    "WanIp": "",
    "InspectInterval": 5
  }
}
```

Common fields:

| Field | Description |
| --- | --- |
| `Name` | Service name used by discovery, gateway, remote calls, and logs |
| `Protocol` | Service protocol, commonly `HTTP`, `HTTPS`, or `GRPC` |
| `Port` | Listening port |
| `Ip` / `WanIp` | Internal/external address for discovery and gateway scenarios |
| `InspectInterval` | Health-check interval for backends that use it |

Module configuration should stay in module-specific sections such as `ConnectionString`, `Redis`, `Consul`, `Nacos`, `Feign`, and `RabbitMQ`. This keeps module replacement easier.

## Multi-backend abstractions

Many SyZero capabilities follow a "core interface + multiple backends" model:

| Capability | Core abstraction | Common implementations |
| --- | --- | --- |
| Service management | `IServiceManagement` | Local / DB / Redis / Consul / Nacos |
| Event bus | `IEventBus` | Local / DB / Redis / RabbitMQ / Kafka |
| Repository | `IRepository<T>` / `IUnitOfWork` | EF Core / SqlSugar / MongoDB |
| Remote calls | `IRemoteServiceProvider` | Feign HTTP provider |

Business code should depend on core abstractions rather than backend-specific types. This lets you start with local or in-memory implementations and later switch to Redis, Consul, RabbitMQ, or other modules.

## Module extension method conventions

Module registration usually follows this naming style:

```csharp
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddSyZeroSqlSugar<MyDbContext>();
builder.Services.AddSyZeroFeign(builder.Configuration);
builder.Services.AddSyZeroOpenTelemetry(builder.Configuration);
```

Middleware and endpoints usually follow:

```csharp
app.UseSyZeroDiagnostics();
app.MapSyZeroGraphQL();
```

New modules should keep this naming style so users can infer the registration location and module purpose from the method name.

## When to split microservices

SyZero supports monoliths, modular monoliths, and microservices. You do not need to split services on day one. A practical path is:

1. Start with clear business layers and boundaries
2. Put stable contracts in `IApplication`
3. Introduce Feign, service discovery, gateway, and event bus when cross-process calls are needed
4. Add Outbox, distributed transactions, or message queues when consistency requirements increase

If the business is still changing quickly, prefer a modular monolith. Split into microservices when team ownership, deployment, scaling, or fault isolation actually requires it.

## Next

1. Follow [Getting Started](/en/guide/getting-started) to run a minimal service
2. Read [Building a Layered Service](/en/guide/layered-service) for the standard business structure
3. Read [Data Access and Repositories](/en/guide/data-access) to choose a database provider
4. Read [Service Registration and Discovery](/en/guide/service-discovery) to add service governance
