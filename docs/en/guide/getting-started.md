# Getting Started

This guide starts from an empty ASP.NET Core Web API project, boots a minimal SyZero service, and exposes an application service through Dynamic Web API.

## Prerequisites

1. .NET 9 SDK
2. A reachable NuGet feed
3. Local port `5000` available

If you already have an ASP.NET Core project, skip the project creation step and install the SyZero packages directly.

## 1. Create a Web API project

```bash
dotnet new webapi -n MyService
cd MyService
```

## 2. Install packages

The minimal Web API host usually needs:

```bash
dotnet add package SyZero.Core
dotnet add package SyZero.AspNetCore
dotnet add package SyZero.DynamicWebApi
dotnet add package SyZero.Swagger
```

Install database, cache, service discovery, or messaging modules only when your service needs them. SyZero modules are independent NuGet packages.

## 3. Configure Program.cs

Update `Program.cs`:

```csharp
using SyZero;
using SyZero.DynamicWebApi;
using SyZero.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

builder.Services.AddControllers();
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = "MyService"
});
builder.Services.AddSwagger();

var app = builder.Build();

app.UseSyZero();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
```

Key points:

1. `builder.AddSyZero()` registers core services, automatic DI, base cache, validation, and startup hooks
2. `AddDynamicWebApi(...)` exposes matching application services as Web APIs
3. `app.UseSyZero()` sets runtime context and works with the host lifecycle for service registration/unregistration

## 4. Add base configuration

Add the `SyZero` section to `appsettings.json`:

```json
{
  "SyZero": {
    "Name": "MyService",
    "Protocol": "HTTP",
    "Port": 5000,
    "Ip": "",
    "WanIp": ""
  },
  "AllowedHosts": "*"
}
```

`Name` is the service name and is reused by service registration, Feign calls, gateway management, and other modules. `Protocol`, `Ip`, and `Port` describe the service instance address. Fill them in even before enabling service discovery.

## 5. Add an application service

Create `HelloAppService.cs`:

```csharp
using SyZero;
using SyZero.Application.Attributes;
using SyZero.Application.Routing;
using SyZero.Application.Service;
using SyZero.Dependency;

[DynamicApi]
public interface IHelloAppService : IApplicationService, IDynamicApi
{
    [Get("ping")]
    Task<HelloOutput> PingAsync();

    [Post("echo")]
    Task<HelloOutput> EchoAsync(HelloInput input);
}

public class HelloAppService : SyZeroServiceBase, IHelloAppService, IScopedDependency
{
    public Task<HelloOutput> PingAsync()
    {
        return Task.FromResult(new HelloOutput("MyService", "pong"));
    }

    public Task<HelloOutput> EchoAsync(HelloInput input)
    {
        return Task.FromResult(new HelloOutput("MyService", input.Message));
    }
}

public record HelloInput(string Message);

public record HelloOutput(string Service, string Message);
```

The conventions are:

1. The interface inherits `IApplicationService` to mark an application service contract
2. The interface inherits `IDynamicApi` and uses `[DynamicApi]` so the dynamic API module can publish it
3. The implementation inherits `SyZeroServiceBase` to access common capabilities such as `ObjectMapper`, `UnitOfWork`, and `Logger`
4. The implementation uses `IScopedDependency` for automatic DI registration

## 6. Run and verify

```bash
dotnet run --urls http://localhost:5000
```

Open Swagger:

```text
http://localhost:5000/swagger
```

You should see the generated `Hello` endpoints. With the configuration above, the common generated paths look like:

```text
GET  /api/myservice/hello/ping
POST /api/myservice/hello/echo
```

If the endpoints do not appear in Swagger, check:

1. `AddDynamicWebApi(...)` is called after `AddControllers()`
2. The application service interface inherits `IDynamicApi`
3. The interface or implementation is marked with `[DynamicApi]`
4. The implementation can be registered into DI, for example through `IScopedDependency`

## 7. Add more capabilities

After the minimal service works, add modules by scenario:

| Capability | Common modules | Next step |
| --- | --- | --- |
| Layered services | `SyZero.Core` | [Building a Layered Service](/en/guide/layered-service) |
| Data access | `SyZero.EntityFrameworkCore` / `SyZero.SqlSugar` / `SyZero.MongoDB` | [Data Access and Repositories](/en/guide/data-access) |
| Service discovery | `SyZero.ServiceDiscovery.Consul` / `SyZero.ServiceDiscovery.Nacos` / `SyZero.Redis` | [Service Registration and Discovery](/en/guide/service-discovery) |
| Event bus | `SyZero.EventBus.RabbitMQ` / `SyZero.EventBus.Kafka` / `SyZero.Redis` | [Event Bus and Integration Events](/en/guide/event-bus) |
| Gateway | `SyZero.Gateway.Ocelot` / `SyZero.Gateway.YARP` / `SyZero.Gateway.Admin` | [SyZero.Gateway Microservice Gateway Demo](/en/guide/gateway-microservice-demo) |

## FAQ

### Do I need to call AddScoped manually

Usually no. Types implementing `IScopedDependency`, `ISingletonDependency`, or `ITransientDependency` are registered automatically. Use manual registration only when overriding defaults, registering third-party types, or binding open generics.

### Does Dynamic Web API expose every class

No. With the current convention, a service type must satisfy the dynamic API filters, such as implementing `IDynamicApi`, having `[DynamicApi]`, and not being excluded by `[NonDynamicApi]` or `[NonWebApiService]`.

### Is app.UseSyZero required

Yes, keep it in the host pipeline. It sets the SyZero runtime service provider. Service registration/unregistration is handled asynchronously by `SyZeroBootstrapper` through `IHostedService`, but `UseSyZero()` remains the standard host integration step.

## Next

1. Read [Architecture](/en/guide/architecture)
2. Read [Building a Layered Service](/en/guide/layered-service)
3. Read [Data Access and Repositories](/en/guide/data-access)
4. Read [Service Registration and Discovery](/en/guide/service-discovery)
5. Read [Event Bus and Integration Events](/en/guide/event-bus)
6. Explore [Modules](/en/modules/index)
