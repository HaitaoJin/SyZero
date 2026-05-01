# SyZero.Mapster

SyZero 框架的 [Mapster](https://github.com/MapsterMapper/Mapster) 集成模块，提供轻量、高性能的对象映射能力。

> 📌 SyZero.Mapster 与 [SyZero.AutoMapper](./SyZero.AutoMapper.md) 都实现了 `SyZero.ObjectMapper.IObjectMapper` 抽象。
> 业务代码注入 `IObjectMapper` 即可，**切换实现无需修改业务层**。

## 📦 安装

```bash
dotnet add package SyZero.Mapster
```

## ✨ 特性

- 🚀 **自动扫描** - 自动扫描程序集中所有 `IRegister` 实现，注册映射规则
- 💾 **依赖注入** - 与 `IServiceCollection` 无缝集成，DI 解析 `IMapper` 与 `IObjectMapper`
- 🔒 **MIT 许可** - 无商业授权风险（区别于 AutoMapper 14+）
- 🪶 **极简依赖** - 仅依赖 `Mapster` + `Mapster.DependencyInjection`，零第三方拖累
- ⚡ **高性能** - Mapster 在编译期生成 IL，无反射开销
- 🎯 **目标框架** - `net9.0`

---

## 🚀 快速开始

### 1. 注册服务

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// 添加 SyZero
builder.AddSyZero();

// 方式 1 - 自动扫描所有程序集中的 IRegister
builder.Services.AddSyZeroMapster();

// 方式 2 - 仅扫描指定程序集
builder.Services.AddSyZeroMapster(typeof(UserMappingProfile).Assembly);

// 方式 3 - 自定义 TypeAdapterConfig
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

### 2. 编写映射规则（可选）

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

> Mapster 默认启用约定映射（同名同类型自动匹配），**多数情况下无需手写 Profile**。

### 3. 在业务代码中使用

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
        // 通过 SyZero.ObjectMapper.IObjectMapper 进行映射
        return ObjectMapper.Map<UserDto>(user);
    }
}
```

---

## 🔄 从 SyZero.AutoMapper 迁移

| 步骤 | 操作 |
| --- | --- |
| 1 | `dotnet remove package SyZero.AutoMapper` |
| 2 | `dotnet add package SyZero.Mapster` |
| 3 | `AddSyZeroAutoMapper(...)` → `AddSyZeroMapster(...)` |
| 4 | `Profile` 子类 → `IRegister` 实现（或直接删除，使用约定映射） |
| 5 | 业务代码使用 `IObjectMapper`，**无需改动** |

---

## 📚 参考

- [Mapster GitHub](https://github.com/MapsterMapper/Mapster)
- [Mapster Wiki](https://github.com/MapsterMapper/Mapster/wiki)
- [SyZero.AutoMapper](./SyZero.AutoMapper.md)（旧）
