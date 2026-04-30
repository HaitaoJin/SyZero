---
description: SyZero 业务服务（src/SyZero.Service/**）的分层与命名约束
applyTo: "src/SyZero.Service/**/*.cs,src/SyZero.Service/**/*.csproj"
---

# 业务服务约束

适用范围：[src/SyZero.Service/](../../src/SyZero.Service) 下的业务服务示例与新建业务服务。参考实现：[SyZero.Example1.Service](../../src/SyZero.Service/SyZero.Example1.Service)。

## 强制分层（依赖方向单向）

```
SyZero.<Biz>.Web → SyZero.<Biz>.Application → SyZero.<Biz>.IApplication → SyZero.<Biz>.Core
```

| 层 | 职责 | 不得做 |
|---|---|---|
| `Web` | Program.cs / 控制器 / 过滤器 / appsettings / Dockerfile | 不得直接依赖 `Application` 实现，应通过 `IApplication` 接口 |
| `Application` | 业务服务实现，继承 `SyZeroServiceBase`，实现 `I*Service`，注入仓储 | 不得引用 `Web`；不得包含 HTTP 关注点 |
| `IApplication` | 业务接口（`: IApplicationService`）+ DTO | 不得包含实现 / EF / SqlSugar / 任何基础设施引用 |
| `Core` | 领域实体、领域服务、枚举、领域事件 | 不得引用上层；不得引用 ASP.NET Core |

## 必须遵守

- 命名严格遵循 `SyZero.<Biz>.<Layer>` 模式（`<Biz>` 与解决方案目录名一致）。
- 业务接口继承 [IApplicationService](../../src/SyZero.Core/Application/IApplicationService.cs)；实现继承 `SyZeroServiceBase` 并标记 `IScopedDependency`（自动注册，**不要**手动 `AddScoped`）。
- DTO 放在 `IApplication` 层；AutoMapper Profile 放在 `Application/MapProfile/`，命名 `<Entity>Profile.cs`。
- `Program.cs` 必须 `builder.AddSyZero()` 与 `app.UseSyZero()`（触发服务注册到 `IServiceManagement`）。
- `appsettings.json` 必须含 `SyZero` 节点（`Name`/`Protocol`/`Port`/`Ip`/`WanIp`），见 [README.md](../../README.md) 配置示例。
- 选择数据访问后端（EF Core / SqlSugar / MongoDB）后，仅注册一个 `IRepository<T>` 实现，避免冲突。

## 不要做

- 不要在 `PackageReference` 上写 `Version`（中央版本管理，见 [Directory.Packages.props](../../Directory.Packages.props)）。
- 不要直接引用 `src/SyZero.Core/**/bin/` 下的 dll，统一通过 `<ProjectReference>`。
- 不要把领域逻辑写进 `Web` 控制器；控制器只做参数绑定 → 调用 `I*Service`。
- 不要把基础设施（DbContext、SqlSugarClient、Redis 客户端）泄漏到 `IApplication` / `Core`。
- 不要在业务服务里手动注册 `IScopedDependency` / `ISingletonDependency` / `ITransientDependency` 实现类——会与自动注册重复。
