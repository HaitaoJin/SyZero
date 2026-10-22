# SyZero.ObjectMapping.Mapster

The [Mapster](https://github.com/MapsterMapper/Mapster) integration module for the SyZero framework, providing lightweight, high-performance object mapping.

> 📌 Both `SyZero.ObjectMapping.Mapster` and [`SyZero.ObjectMapping.AutoMapper`](./SyZero.ObjectMapping.AutoMapper.md) implement the `SyZero.ObjectMapper.IObjectMapper` abstraction.
> Inject `IObjectMapper` in your code and the implementation can be swapped without touching business logic.

## 📦 Install

```bash
dotnet add package SyZero.ObjectMapping.Mapster
```

## ✨ Features

- 🚀 **Auto scan** — discovers every `IRegister` implementation across the loaded assemblies
- 💾 **DI integration** — registers `IMapper` and `IObjectMapper` directly into `IServiceCollection`
- 🔒 **MIT licensed** — no commercial-license risk (unlike AutoMapper 14+)
- 🪶 **Minimal deps** — only `Mapster` + `Mapster.DependencyInjection`
- ⚡ **High performance** — Mapster emits IL at compile-time, no reflection overhead
- 🎯 **Target framework** — `net9.0`

---

## 🚀 Quick start

### 1. Register the service

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

// Option 1 — auto scan all loaded assemblies for IRegister
builder.Services.AddSyZeroMapster();

// Option 2 — scan only specific assemblies
builder.Services.AddSyZeroMapster(typeof(UserMappingProfile).Assembly);

// Option 3 — customize TypeAdapterConfig
builder.Services.AddSyZeroMapster(config =>
{
    config.Default.IgnoreNullValues(true);
    config.NewConfig<User, UserDto>()
        .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
}, typeof(UserMappingProfile).Assembly);

var app = builder.Build();
app.UseSyZero();
app.Run();
```

### 2. Define mapping rules (optional)

```csharp
public class UserMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
            .Map(dest => dest.Age, src => DateTime.UtcNow.Year - src.BirthYear);
    }
}
```

> Mapster ships with convention-based mapping (same name + same type are matched automatically), so a `Profile` is rarely required.

### 3. Use it in business code

```csharp
public class UserAppService : SyZeroServiceBase, IUserAppService
{
    private readonly IRepository<User> _userRepository;

    public UserAppService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto> GetAsync(long id)
    {
        var user = await _userRepository.GetAsync(id);
        // Map via the SyZero.ObjectMapper.IObjectMapper abstraction
        return ObjectMapper.Map<UserDto>(user);
    }
}
```

---

## 🔄 Migrating from SyZero.ObjectMapping.AutoMapper

| Step | Action |
| --- | --- |
| 1 | `dotnet remove package SyZero.ObjectMapping.AutoMapper` |
| 2 | `dotnet add package SyZero.ObjectMapping.Mapster` |
| 3 | `AddSyZeroAutoMapper(...)` → `AddSyZeroMapster(...)` |
| 4 | Convert `Profile` subclasses to `IRegister` implementations (or delete them and rely on convention mapping) |
| 5 | Business code keeps using `IObjectMapper` — **no changes needed** |

---

## 📚 References

- [Mapster on GitHub](https://github.com/MapsterMapper/Mapster)
- [Mapster Wiki](https://github.com/MapsterMapper/Mapster/wiki)
- [`SyZero.ObjectMapping.AutoMapper`](./SyZero.ObjectMapping.AutoMapper.md) (legacy)
- Chinese documentation: [/modules/SyZero.ObjectMapping.Mapster](/modules/SyZero.ObjectMapping.Mapster)
