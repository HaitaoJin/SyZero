# SyZero.MultiTenancy

SyZero multi-tenancy module: `ICurrentTenant` / `ITenantStore` / `ITenantResolver` (Header / Domain / Claim) + `MultiTenancyMiddleware`. Default strategy is single-database with EF Core `HasQueryFilter`; the `ITenantConnectionResolver` extension point keeps the door open for sharding.

## Design

| Capability | Implementation |
| --- | --- |
| Current tenant context | `ICurrentTenant` + `CurrentTenant` (Scoped, backed by `AsyncLocal<TenantInfo?>`). `Change(tenant)` returns `IDisposable` so background workers can flow context manually. |
| Tenant store | `ITenantStore` with `FindByIdAsync` / `FindByNameAsync`. Built-in `InMemoryTenantStore` for demos / unit tests. |
| Resolution chain | `ITenantResolver` instances tried in registration order; first match wins. Built-ins: `HeaderTenantResolver` (`__tenant` by default), `DomainTenantResolver` (sub-domain via `DomainSuffix`), `ClaimTenantResolver` (`tenantid` / `tenant` claims). |
| Middleware | `MultiTenancyMiddleware` runs all resolvers → calls `ITenantStore` → enters `ICurrentTenant.Change(tenant)` and restores on response end. |
| Data isolation | Entities implement `IMultiTenant` (`long? TenantId`); business `DbContext` wires global query filter using `ICurrentTenant`. |
| Sharding hook | `ITenantConnectionResolver` (default reads `TenantInfo.ConnectionString`). |

## Quick start

```csharp
builder.Services
    .AddSyZero()
    .AddSyZeroAuthorization(builder.Configuration)
    .AddSyZeroMultiTenancy(builder.Configuration);

builder.Services.AddSingleton<ITenantStore, InMemoryTenantStore>();

var app = builder.Build();
app.UseSyZeroAuthorization();
app.UseSyZeroMultiTenancy();
app.MapControllers();
app.Run();
```

`appsettings.json`:

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

## Marking entities

```csharp
public class Order : Entity, IMultiTenant
{
    public long? TenantId { get; set; }
    public decimal Amount { get; set; }
}
```

## EF Core query filter

```csharp
modelBuilder.Entity<Order>().HasQueryFilter(o =>
    !_currentTenant.Id.HasValue || o.TenantId == _currentTenant.Id);
```

Auto-fill `TenantId` on insert in `SaveChanges` by iterating `ChangeTracker.Entries<IMultiTenant>()`.

## Manual context switch (background work)

```csharp
var tenant = await _store.FindByNameAsync("acme");
using (_currentTenant.Change(tenant))
{
    // DbContext access here is filtered to the acme tenant
}
```

## Sharding extension

```csharp
public class ShardingTenantConnectionResolver : ITenantConnectionResolver
{
    public Task<string?> ResolveAsync(TenantInfo? tenant, CancellationToken ct = default)
    {
        if (tenant == null) return Task.FromResult<string?>("Server=host;Database=app_main;...");
        var shard = tenant.Id % 4;
        return Task.FromResult<string?>($"Server=host;Database=app_shard{shard};...");
    }
}
```

## Compatibility

- Target framework: `net9.0`
- Dependencies: `SyZero.Core`, `Microsoft.AspNetCore.App` (FrameworkReference)
- `ICurrentTenant.Change` flows across `await` thanks to `AsyncLocal<T>`.

## Known limitations

- SqlSugar global filters are wired differently from EF Core; this module does not provide helpers — inject `ICurrentTenant` and use `db.QueryFilter.Add(...)` in your business code.
- No built-in "cross-tenant query" toggle; call `_currentTenant.Change(null)` to enter the host-side context when needed.
