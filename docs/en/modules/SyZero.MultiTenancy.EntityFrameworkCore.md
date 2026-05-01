# SyZero.MultiTenancy.EntityFrameworkCore

EF Core persistence backend for `SyZero.MultiTenancy`.

## 📦 Install

```bash
dotnet add package SyZero.MultiTenancy.EntityFrameworkCore
```

## ✨ Features

- 🗄️ Out-of-the-box `TenantEntity` + `TenantDbContext` (`SyTenants` table, unique `Name` index).
- 🔌 `EfCoreTenantStore` replaces the default `InMemoryTenantStore`.

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroMultiTenancy(builder.Configuration);
builder.Services.AddSyZeroMultiTenancyEfCore(opt =>
    opt.UseSqlite("DataSource=tenants.db"));

app.UseSyZeroMultiTenancy();
```

`AddSyZeroMultiTenancyEfCore` **replaces** the `ITenantStore` registration, overriding the in-memory default. Manage migrations with `dotnet ef migrations` from your application.
