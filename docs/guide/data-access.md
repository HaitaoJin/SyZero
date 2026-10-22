# 数据访问与仓储

SyZero 把仓储接口放在核心包，把具体实现拆到独立模块里。业务层只依赖 `IRepository<>` / `IUnitOfWork`，部署时再选择 EF Core、SqlSugar 或 MongoDB。

## 该选哪个 Provider

| Provider | 模块 | 适用场景 |
| --- | --- | --- |
| EF Core | `SyZero.EntityFrameworkCore` | 已有 EF Core 经验，需要迁移、LINQ 生态和 DbContext 集成 |
| SqlSugar | `SyZero.SqlSugar` | 想快速接入关系型数据库，偏向轻量配置和更直接的 ORM 体验 |
| MongoDB | `SyZero.MongoDB` | 文档型数据、非关系型存储 |

如果你是从示例项目起步，仓库里的 `Example1`、`Example2`、`Example3` 默认都偏向 `SqlSugar` 路径。

## 1. 定义实体

默认主键可以直接使用 `Entity<long>`：

```csharp
public class User : Entity<long>
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
}
```

如果你需要 `Guid`、`string` 等主键，也可以直接使用泛型版本：

```csharp
public class Order : Entity<Guid>
{
    public decimal TotalAmount { get; set; }
}
```

## 2. 接入 Entity Framework Core

先安装模块：

```bash
dotnet add package SyZero.EntityFrameworkCore
```

注册方式：

```csharp
builder.AddSyZero();

builder.Services.AddSyZeroEntityFramework<MyDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
```

`DbContext` 示例：

```csharp
public class MyDbContext : SyZeroDbContext<MyDbContext>
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
```

适合场景：

1. 你需要 EF Core Migration
2. 你已经有现成的 `DbContext` 和模型配置
3. 你希望把多租户、Outbox 等 EF 相关模块串起来

## 3. 接入 SqlSugar

先安装模块：

```bash
dotnet add package SyZero.SqlSugar
```

注册方式：

```csharp
builder.AddSyZero();

builder.Services.AddSyZeroSqlSugar<MyDbContext>();
```

配置示例：

```json
{
  "ConnectionString": {
    "Type": "Sqlite",
    "Master": "Data Source=app.db"
  }
}
```

自定义上下文：

```csharp
public class MyDbContext : SyZeroDbContext
{
    public MyDbContext(ConnectionConfig config, ILoggerFactory loggerFactory)
        : base(config, loggerFactory)
    {
    }
}
```

示例项目里常见的 `app.InitTables()` 更适合 demo 或本地快速验证；正式业务更建议把建表、迁移策略独立管理。

## 4. 在应用服务中使用仓储

应用层不需要知道底层究竟是 EF Core 还是 SqlSugar，只依赖抽象接口即可。

```csharp
public class UserAppService : SyZeroServiceBase, IUserAppService, IScopedDependency
{
    private readonly IRepository<User, long> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserAppService(
        IRepository<User, long> userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<long> CreateAsync(CreateUserInput input)
    {
        var user = ObjectMapper.Map<User>(input);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var entity = await _userRepository.InsertAsync(user);
            await _unitOfWork.CommitAsync();
            return entity.Id;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}
```

## 5. 自定义仓储

当通用仓储不够时，可以在 `Core` 或 `Application` 中继续封装自定义接口。

```csharp
public interface IUserRepository : IRepository<User, long>
{
    Task<User> GetByNameAsync(string name);
}
```

建议：

1. 通用 CRUD 继续走 `IRepository<>`
2. 查询拼装复杂、存在明确聚合边界时，再引入专用仓储接口
3. 自定义仓储仍然尽量向业务暴露语义，而不是泄露底层 ORM 细节

## 6. 选择建议

1. 想快速跟随示例项目落地，可优先用 `SyZero.SqlSugar`
2. 已有 EF Core 体系或计划接入 EF 版多租户、Outbox，可优先用 `SyZero.EntityFrameworkCore`
3. 不要在应用服务里直接绑定具体 ORM 类型，尽量停留在仓储与工作单元抽象层

## 下一步

1. 继续阅读 [服务注册与发现](/guide/service-discovery)
2. 继续阅读 [事件总线与集成事件](/guide/event-bus)
3. 具体 Provider API 请查看 [模块总览](/modules/index)
