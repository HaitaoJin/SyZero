---
description: SyZero 框架模块（src/SyZero.Module/**）与内核（src/SyZero.Core/**）的编写约束
applyTo: "src/SyZero.Core/**/*.cs,src/SyZero.Core/**/*.csproj,src/SyZero.Module/**/*.cs,src/SyZero.Module/**/*.csproj"
---

# 框架模块约束

适用范围：[src/SyZero.Core/](../../src/SyZero.Core)（核心包 `SyZero.Core`）与 [src/SyZero.Module/](../../src/SyZero.Module)（可插拔模块包）。详见 [AGENTS.md](../../AGENTS.md)。

## 必须遵守

- **禁止**在 `<PackageReference>` 上写 `Version`。统一在 [Directory.Packages.props](../../Directory.Packages.props) 增加 `<PackageVersion>`。
- 新建模块项目时：
  1. 路径必须是 `src/SyZero.Module/SyZero.<Name>/SyZero.<Name>.csproj`。
  2. csproj 中设置 `<IsPackable>true</IsPackable>`（`Directory.Build.props` 默认为 false）。
  3. 同步创建 [docs/modules/SyZero.\<Name\>.md](../../docs/modules/) ——它会被 `Directory.Build.props` 打包为 NuGet 的 README，缺失会导致包内无 README。
  4. 在 [src/SyZero.Tests/SyZero.Tests.csproj](../../src/SyZero.Tests/SyZero.Tests.csproj) 添加 `<ProjectReference>` 并新增对应 `<Module>Tests.cs`。
- 公共扩展方法命名固定：注册用 `AddSyZero<Module>(...)` 或 `Add<Module>()`，中间件用 `Use<Module>()`。文件名 `<Module>Extension.cs`，参考 [src/SyZero.Core/SyZeroExtension.cs](../../src/SyZero.Core/SyZeroExtension.cs)。
- 抽象（接口）保留在核心 `SyZero.Core` 包；不同后端的实现下沉到独立模块包（参照 `IServiceManagement` / `IEventBus` / `IRepository<T>` 的拆分）。
- 实现 `IScopedDependency` / `ISingletonDependency` / `ITransientDependency` 即可被自动注册——**不要**再手动 `services.AddScoped<...>()` 注册同一类型。

## 不要做

- 不要启用 `<Nullable>enable</Nullable>`，除非整个模块全部就绪——框架模块默认未开。
- 不要修改 [Directory.Build.props](../../Directory.Build.props) 中的 `<Version>`，那会改动**所有**包版本。
- 不要破坏既有 `Add*/Use*` 公共方法签名——它们是下游使用者的稳定 API，必须同时更新对应 `docs/modules/<Module>.md`。
- 不要在框架包里直接引用业务示例（`SyZero.Service` / `SyZero.Gateway`）。
