# 创建分层业务服务

SyZero 推荐把业务服务拆成清晰的四层，这样既能复用框架约定，也方便后续拆分微服务或替换基础设施。

## 推荐目录

以 `SyZero.Example1.Service` 为例，建议保持下面的结构：

```text
SyZero.Example1.Service/
├─ SyZero.Example1.Web/           # ASP.NET Core 宿主、控制器、Swagger、中间件
├─ SyZero.Example1.Application/   # 应用服务实现、用例编排、对象映射
├─ SyZero.Example1.IApplication/  # 应用服务接口、DTO、对外契约
├─ SyZero.Example1.Core/          # 实体、领域对象、仓储扩展、数据库上下文
└─ app/                           # IApplication.dll 汇集目录
```

依赖方向建议固定为：

1. `Web` 引用 `Application` 和 `IApplication`
2. `Application` 引用 `IApplication` 和 `Core`
3. `IApplication` 只放契约，不引用 `Web`
4. `Core` 保持最小依赖，避免反向依赖上层

## 1. 在 IApplication 定义服务契约

`IApplication` 层负责定义对外暴露的能力。动态 Web API、Feign、gRPC 之类的能力都更适合围绕这层展开。

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

要点：

1. 接口继承 `IApplicationService`
2. DTO 放在这一层，避免把数据库实体直接暴露给调用方
3. 契约名称尽量稳定，因为动态 API 和远程调用会直接消费它

## 2. 在 Core 放实体与基础模型

`Core` 层放实体、值对象、领域服务，以及与具体存储实现无关的业务模型。

```csharp
public class User : Entity<long>
{
    public string Name { get; set; }
}
```

如果业务需要非 `long` 主键，可以直接使用泛型实体：

```csharp
public class TenantUser : Entity<Guid>
{
    public string Name { get; set; }
}
```

## 3. 在 Application 实现应用服务

应用服务负责业务编排。SyZero 的常见写法是继承 `SyZeroServiceBase`，并通过生命周期接口实现自动注入。

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

这里有几个关键约定：

1. 实现 `IScopedDependency` / `ISingletonDependency` / `ITransientDependency` 会自动注册到 DI
2. 继承 `SyZeroServiceBase` 后可以直接使用 `ObjectMapper`、`SySession` 等基础能力
3. 业务接口与实现分离后，更方便被动态 API 与远程调用复用

## 4. 在 Web 宿主中接入框架能力

`Web` 层负责把业务服务挂到 HTTP 宿主上。

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

注意：

1. `AddDynamicWebApi(...)` 必须放在 `AddControllers()` 或 `AddMvc()` 之后
2. `builder.AddSyZero()` 负责注册核心约定
3. `app.UseSyZero()` 会触发框架启动期初始化，并配合服务管理完成注册/注销

## 5. `app/` 目录的作用

示例服务里保留了 `app/` 目录，用于汇集 `IApplication` 程序集输出。这一约定适合下面两类场景：

1. 宿主统一扫描多个业务契约程序集
2. 服务拆分后仍希望沿用同一套程序集发现方式

如果你沿用 `Example1` / `Example2` 的模板，新业务服务也建议保留这一结构。

## 常见落地顺序

1. 先在 `IApplication` 定义契约和 DTO
2. 再在 `Core` 定义实体与仓储相关模型
3. 然后在 `Application` 写业务实现
4. 最后在 `Web` 接入 DynamicWebApi、Swagger、仓储 Provider 等模块

## 下一步

1. 继续阅读 [数据访问与仓储](/guide/data-access)
2. 继续阅读 [服务注册与发现](/guide/service-discovery)
3. 如需前后端同宿主，可参考 [Example3.SPA 单体宿主示例](/guide/example3-spa)
