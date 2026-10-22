# SyZero.MultiTenancy.EntityFrameworkCore

为 `SyZero.MultiTenancy` 提供 EF Core 持久化的租户存储实现。

## 📦 安装

```bash
dotnet add package SyZero.MultiTenancy.EntityFrameworkCore
```

## ✨ 特性

- 🗄️ **`TenantEntity` + `TenantDbContext`**：开箱即用的租户元数据表（`SyTenants`，`Name` 唯一索引）。
- 🔌 **`EfCoreTenantStore`**：替换默认 `InMemoryTenantStore`，落库存储。
- 🧬 **可继承扩展**：业务可派生 `TenantEntity` 添加自定义字段，或把 `DbSet<TenantEntity>` 合入主 DbContext。

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroMultiTenancy(builder.Configuration);
builder.Services.AddSyZeroMultiTenancyEfCore(opt =>
    opt.UseSqlite("DataSource=tenants.db"));

app.UseSyZeroMultiTenancy();
```

## 🧩 注意

- `AddSyZeroMultiTenancyEfCore` 会**替换** `ITenantStore` 注册，覆盖默认 `InMemoryTenantStore`。
- 数据库迁移需业务侧用 `dotnet ef migrations` 自行管理（依赖你的 DbContext 提供方式）。
