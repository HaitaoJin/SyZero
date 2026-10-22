# SyZero.Authorization.AspNetCore

把 ASP.NET Core 授权策略 `[Authorize(Policy = "...")]` 桥接到 SyZero 的 `IPermissionChecker`，避免在控制器里手写 `if (!_checker.IsHas(...))`。

## 📦 安装

```bash
dotnet add package SyZero.Authorization.AspNetCore
```

## ✨ 特性

- 🔌 **零侵入**：业务用 `[Authorize(Policy = SyPermissionPolicy.For("Order.Create"))]` 即可，权限校验仍走 `IPermissionChecker`。
- 🧩 **复用 RBAC**：默认与 `SyZero.Authorization` 的 `RbacPermissionChecker` 配合，无需额外配置。
- 🛡️ **未授权返回 403**：由 ASP.NET Core 授权管线统一处理，与其它策略保持一致。

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroAuthorizationBridge();

builder.Services.AddAuthorization(options =>
{
    options.AddSyPermission("Order.Create");
    options.AddSyPermission("Order.Delete");
});

// Controller
[Authorize(Policy = "syperm:Order.Create")]
public Task<int> Create(...) { ... }
```

或使用 `SyPermissionPolicy.For("Order.Create")` 拼接策略名。

## 🧩 自定义 IPermissionChecker

无需任何改动：业务侧替换 `IPermissionChecker` 实现（例如对接 OPA / 自定义 RBAC）即可立即生效。
