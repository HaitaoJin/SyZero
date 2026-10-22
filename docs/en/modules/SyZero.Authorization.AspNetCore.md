# SyZero.Authorization.AspNetCore

Bridges ASP.NET Core authorization policies to SyZero's `IPermissionChecker`, so controllers can use `[Authorize(Policy = "...")]` instead of writing `if (!_checker.IsHas(...))` manually.

## 📦 Install

```bash
dotnet add package SyZero.Authorization.AspNetCore
```

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroAuthorizationBridge();
builder.Services.AddAuthorization(o => o.AddSyPermission("Order.Create"));

[Authorize(Policy = "syperm:Order.Create")]
public Task<int> Create(...) { ... }
```

Use `SyPermissionPolicy.For("Order.Create")` to construct the policy name. Replacing `IPermissionChecker` (e.g. with OPA / custom RBAC) takes effect immediately without code changes.
