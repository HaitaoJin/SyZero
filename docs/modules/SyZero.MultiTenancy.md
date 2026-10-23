# SyZero.MultiTenancy

SyZero 多租户模块。提供 `ICurrentTenant` / `ITenantStore` / `ITenantResolver`（Header / Domain / Claim 三种）+ `MultiTenancyMiddleware`，默认按「单库 + Filter」策略落地，抽象保留可扩展到分库（`ITenantConnectionResolver`）。

## 设计概览

| 能力 | 实现 |
|---|---|
| 当前租户上下文 | `ICurrentTenant` + `CurrentTenant`（基于 `AsyncLocal<TenantInfo?>`，`Scoped`）。`Change(tenant)` 返回 `IDisposable`，便于后台任务手工切上下文。 |
| 租户存储 | `ITenantStore`（`FindByIdAsync` / `FindByNameAsync`）。内置 `InMemoryTenantStore`（用于演示 / 单元测试）。 |
| 租户解析 | `ITenantResolver` 多个实现按注册顺序尝试，命中即停。内置 `HeaderTenantResolver`（默认 Header `__tenant`） / `DomainTenantResolver`（按 `DomainSuffix` 取子域） / `ClaimTenantResolver`（取 Claim `tenantid` / `tenant`）。 |
| 中间件 | `MultiTenancyMiddleware` 跑全部 Resolver → 调 `ITenantStore` 取 `TenantInfo` → 写入 `ICurrentTenant`，请求结束自动还原。 |
| 数据隔离 | 实体实现 `IMultiTenant`（`long? TenantId`），由业务侧 `DbContext.OnModelCreating` 配合 `ICurrentTenant` 加 `HasQueryFilter` 实现单库过滤。 |
| 分库扩展点 | `ITenantConnectionResolver`（默认实现读 `TenantInfo.ConnectionString`），业务可替换为按 Id 路由到不同连接池 / Sharding 中心。 |

## 快速接入

```csharp
// Program.cs
builder.Services
    .AddSyZero()
    .AddSyZeroAuthorization(builder.Configuration)            // Claim 解析需要已认证 User
    .AddSyZeroMultiTenancy(builder.Configuration);            // 注册中间件 + 三种 Resolver

// 业务侧自行实现 ITenantStore，或先用内置内存版做演示
builder.Services.AddSingleton<ITenantStore, InMemoryTenantStore>();

var app = builder.Build();
app.UseSyZeroAuthorization();   // 必须在多租户中间件之前
app.UseSyZeroMultiTenancy();    // 必须在业务 Endpoint 之前
app.MapControllers();
app.Run();
```

`appsettings.json`：

```json
{
  "MultiTenancy": {
    "IsEnabled": true,
    "TenantHeaderName": "__tenant",
    "DomainSuffix": ".myapp.com",
    "TenantIdClaimType": "tenantid",
    "TenantNameClaimType": "tenant"
  }
}
```

## 实体侧标记

```csharp
public class Order : Entity, IMultiTenant
{
    public long? TenantId { get; set; }
    public decimal Amount { get; set; }
}
```

## EF Core 全局过滤器

```csharp
public class MyDbContext : SyZeroDbContext<MyDbContext>
{
    private readonly ICurrentTenant _currentTenant;

    public MyDbContext(DbContextOptions<MyDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 仅返回当前租户数据；宿主侧（_currentTenant.Id == null）放行全部
        modelBuilder.Entity<Order>().HasQueryFilter(o =>
            !_currentTenant.Id.HasValue || o.TenantId == _currentTenant.Id);
    }

    public override int SaveChanges()
    {
        FillTenantId();
        return base.SaveChanges();
    }

    private void FillTenantId()
    {
        var tid = _currentTenant.Id;
        foreach (var entry in ChangeTracker.Entries<IMultiTenant>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == null)
                entry.Entity.TenantId = tid;
        }
    }
}
```

## 业务侧手工切换上下文（后台任务）

```csharp
public class TenantBatchJob
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantStore _store;

    public async Task RunAsync()
    {
        var tenant = await _store.FindByNameAsync("acme");
        using (_currentTenant.Change(tenant))
        {
            // 这里访问 DbContext 时，全局过滤器自动按 acme 租户隔离
        }
    }
}
```

## 分库扩展点

```csharp
public class ShardingTenantConnectionResolver : ITenantConnectionResolver
{
    public Task<string?> ResolveAsync(TenantInfo? tenant, CancellationToken ct = default)
    {
        // 例：按租户 Id 路由到 N 个分库
        if (tenant == null) return Task.FromResult<string?>("Server=host;Database=app_main;...");
        var shard = tenant.Id % 4;
        return Task.FromResult<string?>($"Server=host;Database=app_shard{shard};...");
    }
}

builder.Services.AddSingleton<ITenantConnectionResolver, ShardingTenantConnectionResolver>();
```

EF Core 侧把它接到 `DbContextOptionsBuilder`：

```csharp
services.AddDbContext<MyDbContext>((sp, opt) =>
{
    var current = sp.GetRequiredService<ICurrentTenant>();
    var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
    var conn = resolver.ResolveAsync(current.Tenant).GetAwaiter().GetResult();
    opt.UseSqlServer(conn);
});
```

> 注意：`AddDbContext` 默认 Scoped，每次请求会按当前租户解析连接。

## 兼容性

- 目标框架：`net10.0`
- 依赖：`SyZero.Core`、`Microsoft.AspNetCore.App`（FrameworkReference）
- `ICurrentTenant.Change(...)` 使用 `AsyncLocal<T>`，跨 `await` 安全。

## 已知限制

- SqlSugar 全局过滤器的接入方式与 EF Core 不同（`db.QueryFilter.Add(...)`），本模块不内置帮助方法，业务侧自行注入 `ICurrentTenant` 注册过滤即可。
- 没有内置「跨租户查询」开关；如有需要请在调用点 `_currentTenant.Change(null)` 进入宿主侧上下文。
