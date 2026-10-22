# Building a Layered Service

SyZero works best when service code is split into clear layers. That keeps business code reusable and makes it easier to evolve from a single host into multiple services later.

## Recommended layout

Using `SyZero.Example1.Service` as the reference shape:

```text
SyZero.Example1.Service/
├─ SyZero.Example1.Web/           # ASP.NET Core host, controllers, Swagger, middleware
├─ SyZero.Example1.Application/   # Application service implementations and orchestration
├─ SyZero.Example1.IApplication/  # Contracts and DTOs
├─ SyZero.Example1.Core/          # Entities, domain objects, repository-related models
└─ app/                           # Aggregated IApplication.dll output
```

Recommended dependency flow:

1. `Web` references `Application` and `IApplication`
2. `Application` references `IApplication` and `Core`
3. `IApplication` contains contracts only
4. `Core` stays independent from upper layers

## 1. Define contracts in IApplication

This is the best place for service contracts consumed by DynamicWebApi, Feign, or other remote-access modules.

```csharp
public interface IUserAppService : IApplicationService
{
    Task<UserDto> GetAsync(long id);
    Task<long> CreateAsync(CreateUserInput input);
}

public class UserDto
{
    public long Id { get; set; }
    public string Name { get; set; }
}
```

Rules of thumb:

1. Let service interfaces inherit `IApplicationService`
2. Keep DTOs in this layer instead of leaking persistence entities
3. Treat these contracts as stable, because API exposure and remote calls depend on them

## 2. Put entities into Core

`Core` is the place for entities, value objects, and storage-agnostic business models.

```csharp
public class User : Entity<long>
{
    public string Name { get; set; }
}
```

If you need a non-`long` key, use the generic entity directly:

```csharp
public class TenantUser : Entity<Guid>
{
    public string Name { get; set; }
}
```

## 3. Implement application services in Application

The common SyZero pattern is to inherit `SyZeroServiceBase` and implement a DI lifetime marker.

```csharp
public class UserAppService : SyZeroServiceBase, IUserAppService, IScopedDependency
{
    private readonly IRepository<User, long> _userRepository;

    public UserAppService(IRepository<User, long> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> GetAsync(long id)
    {
        var user = await _userRepository.GetAsync(id);
        return ObjectMapper.Map<UserDto>(user);
    }

    public async Task<long> CreateAsync(CreateUserInput input)
    {
        var user = ObjectMapper.Map<User>(input);
        var entity = await _userRepository.InsertAsync(user);
        return entity.Id;
    }
}
```

Important conventions:

1. `IScopedDependency`, `ISingletonDependency`, and `ITransientDependency` are auto-registered
2. `SyZeroServiceBase` gives you access to common capabilities such as `ObjectMapper` and `SySession`
3. Splitting interface and implementation keeps the service reusable for HTTP and remote invocation

## 4. Wire it in the Web host

The `Web` layer is where you expose the service through ASP.NET Core.

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
    DefaultAreaName = "UserService"
});
builder.Services.AddSwagger();
builder.Services.AddSyZeroSqlSugar<ExampleDbContext>();
builder.Services.AddSyZeroAutoMapper();

var app = builder.Build();

app.UseSyZero();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
```

Notes:

1. `AddDynamicWebApi(...)` must be called after `AddControllers()` or `AddMvc()`
2. `builder.AddSyZero()` registers the core conventions
3. `app.UseSyZero()` finishes runtime initialization and works with service management registration

## 5. Why the `app/` folder exists

The sample services keep an `app/` folder to aggregate `IApplication` assemblies. That convention is useful when:

1. A host scans multiple contract assemblies
2. You want to preserve the same discovery pattern while splitting services further

If you follow the Example1 or Example2 template, keeping that folder is a good default.

## Suggested build order

1. Define contracts and DTOs in `IApplication`
2. Define entities and domain models in `Core`
3. Implement use cases in `Application`
4. Expose them from `Web` with DynamicWebApi, Swagger, and a repository provider

## Next

1. Continue with [Data Access and Repositories](/en/guide/data-access)
2. Continue with [Service Registration and Discovery](/en/guide/service-discovery)
3. For a same-host SPA setup, see [Example3.SPA Single-Host Sample](/en/guide/example3-spa)
