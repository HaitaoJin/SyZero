# Data Access and Repositories

SyZero keeps repository abstractions in the core package and ships concrete implementations as separate modules. Business code depends on `IRepository<>` and `IUnitOfWork`, while the storage provider stays replaceable.

## Which provider to choose

| Provider | Module | Good fit |
| --- | --- | --- |
| EF Core | `SyZero.EntityFrameworkCore` | Existing EF Core experience, migrations, DbContext-based workflows |
| SqlSugar | `SyZero.SqlSugar` | Lightweight relational setup and fast onboarding |
| MongoDB | `SyZero.MongoDB` | Document-oriented persistence |

The sample services in this repository mostly follow the `SqlSugar` path.

## 1. Define entities

For the default key type:

```csharp
public class User : Entity<long>
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
}
```

For a custom key type:

```csharp
public class Order : Entity<Guid>
{
    public decimal TotalAmount { get; set; }
}
```

## 2. Use Entity Framework Core

Install:

```bash
dotnet add package SyZero.EntityFrameworkCore
```

Register:

```csharp
builder.AddSyZero();

builder.Services.AddSyZeroEntityFramework<MyDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
```

`DbContext` example:

```csharp
public class MyDbContext : SyZeroDbContext<MyDbContext>
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
}
```

Choose EF Core when:

1. You rely on migrations
2. You already have an EF model and configuration setup
3. You plan to combine it with EF-based modules such as multitenancy or Outbox

## 3. Use SqlSugar

Install:

```bash
dotnet add package SyZero.SqlSugar
```

Register:

```csharp
builder.AddSyZero();

builder.Services.AddSyZeroSqlSugar<MyDbContext>();
```

Configuration example:

```json
{
  "ConnectionString": {
    "Type": "Sqlite",
    "Master": "Data Source=app.db"
  }
}
```

Custom context:

```csharp
public class MyDbContext : SyZeroDbContext
{
    public MyDbContext(ConnectionConfig config, ILoggerFactory loggerFactory)
        : base(config, loggerFactory)
    {
    }
}
```

The sample apps often call `app.InitTables()` for convenience. That is useful for demos and local verification, but production schema management should usually be handled more explicitly.

## 4. Use repositories in application services

Application code should stay provider-agnostic.

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

## 5. Custom repositories

When the generic repository is not enough, add a domain-specific repository interface.

```csharp
public interface IUserRepository : IRepository<User, long>
{
    Task<User> GetByNameAsync(string name);
}
```

Guidelines:

1. Keep plain CRUD on `IRepository<>`
2. Introduce custom repositories for complex queries or clear aggregate semantics
3. Avoid leaking ORM-specific details into upper business layers

## 6. Recommended default

1. Follow `SyZero.SqlSugar` if you want the shortest path from sample to working service
2. Prefer `SyZero.EntityFrameworkCore` if you already depend on EF Core or want EF-based ecosystem modules
3. Keep application code on repository abstractions, not on provider-specific APIs

## Next

1. Continue with [Service Registration and Discovery](/en/guide/service-discovery)
2. Continue with [Event Bus and Integration Events](/en/guide/event-bus)
3. For provider-specific APIs, browse the [Module Index](/en/modules/index)
