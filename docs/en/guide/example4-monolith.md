# Example4.Monolith: Layered Back-End Monolith Sample

`SyZero.Example4.Monolith` shows how to build a monolith service with the recommended **Core / IApplication / Application / Web** layers. It has one Web host and one deployment unit, while still keeping clear code boundaries for small admin systems, internal tools, or early-stage products before service boundaries are stable.

> Source: `src/SyZero.Service/SyZero.Example4.Monolith/`

## Layout

```
SyZero.Example4.Monolith/
├─ SyZero.Example4.Core/             # Domain models, domain contracts, in-process store
├─ SyZero.Example4.IApplication/     # Application service contracts + DTOs (DynamicWebApi)
├─ SyZero.Example4.Application/      # Application service implementations
├─ SyZero.Example4.Web/              # ASP.NET Core host (port 5005)
└─ app/                              # Auto-aggregated IApplication.dll output
```

Here, “monolith” describes runtime and deployment: only `SyZero.Example4.Web` exposes APIs as one process, with no gateway, service discovery, or Feign remote calls. The code still follows the recommended dependency direction: `Web -> Application -> IApplication + Core`.

## Stack

| Capability | Usage |
| --- | --- |
| Web host | ASP.NET Core 9 |
| SyZero core | `builder.AddSyZero()` / `app.UseSyZero()` |
| API exposure | `SyZero.DynamicWebApi` publishes `TodoAppService` |
| Documentation | `SyZero.Swagger`, default path `/api/swagger` |
| Data | In-process `InMemoryTodoStore` |

## Run

```pwsh
cd src/SyZero.Service/SyZero.Example4.Monolith/SyZero.Example4.Web
dotnet run
```

The default port is `5005`:

- `http://localhost:5005/` — service info
- `http://localhost:5005/api/swagger` — Swagger UI
- `GET http://localhost:5005/api/todo/list` — todo list API

## Key Code

`SyZero.Example4.Web/Program.cs` is the single host entry point:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();
builder.Services.AddControllers();
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = AppConfig.ServerOptions.Name
});
builder.Services.AddSwagger();

var app = builder.Build();

app.UseSyZero();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(options => options.RoutePrefix = "api/swagger");

app.Run();
```

Application services in `SyZero.Example4.Application` implement `IScopedDependency`, so SyZero registers them automatically:

```csharp
public class TodoAppService : SyZeroServiceBase, ITodoAppService, IScopedDependency
{
    private readonly ITodoStore _todoStore;

    public TodoAppService(ITodoStore todoStore)
    {
        _todoStore = todoStore;
    }
}
```

The in-memory store implements `ISingletonDependency`, so requests in the same process share todo data:

```csharp
public class InMemoryTodoStore : ITodoStore, ISingletonDependency
{
}
```

## When To Use It

| Scenario | Recommendation |
| --- | --- |
| Small admin system | Start with the monolith and avoid deployment and governance overhead |
| Unstable business boundaries | Keep the recommended project layers first, split services later |
| Internal tool | Prefer in-process calls before adding Feign, gateway, or registry infrastructure |
| Real persistence needed | Replace `InMemoryTodoStore` with EF Core, SqlSugar, or MongoDB repositories |

## Differences From Other Samples

| Sample | Focus |
| --- | --- |
| [Example1 + Example2 Microservice Call Demo](/en/guide/example1-example2-microservices) | Multiple services, Feign calls, service management |
| [SyZero.Gateway Microservice Gateway Demo](/en/guide/gateway-microservice-demo) | Gateway routing, aggregated Swagger, gateway admin |
| [Example3.SPA Single-Host Sample](/en/guide/example3-spa) | React + ASP.NET Core same-origin hosting |
| Example4.Monolith | Back-end monolith, one Web host, code organized with recommended layers |

## Next Steps

1. Use [Data Access and Repositories](/en/guide/data-access) to replace the in-memory store with a database.
2. Read [Building a Layered Service](/en/guide/layered-service) to decide when to split Core / IApplication / Application / Web projects.
3. If the business needs service decomposition, follow [Example1 + Example2 Microservice Call Demo](/en/guide/example1-example2-microservices).
