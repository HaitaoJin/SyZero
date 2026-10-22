---
description: SyZero 仓库 C# 编码风格（适用于全部 .cs 文件）
applyTo: "**/*.cs"
---

# C# 编码风格

适用范围：本仓库所有 `*.cs` 文件。优先级低于范围化指令（`framework-modules` / `business-service` / `gateway` / `tests`）。

## 命名

- 类型 / 方法 / 属性 / 公共字段：`PascalCase`。
- 私有字段：`_camelCase`（下划线前缀）。
- 局部变量 / 参数：`camelCase`。
- 接口：`I` 前缀（`IApplicationService`、`IRepository<T>`）。
- 异步方法：`Async` 后缀（`GetUserAsync`），返回 `Task` / `Task<T>` / `ValueTask<T>`。
- 扩展方法所在类：`<Module>Extension.cs`，方法 `Add<Module>` / `Use<Module>` 风格对齐既有模块。

## 结构

- `using` 指令置于命名空间**外部**（与现有代码保持一致）。
- 命名空间：与目录结构匹配（`SyZero.<Module>.<Folder>`）。
- 一个文件一个公共类型；私有/嵌套类型可同文件。
- 公共 API 必须有 XML 注释（`/// <summary>`）—— `Directory.Build.props` 启用了 `DocumentationFile`，缺注释会触发警告（已在 `NoWarn` 中静默部分，但仍应补全）。

## 异步与异常

- 优先 `async Task` / `async Task<T>`，避免 `async void`（事件处理器除外）。
- 调用异步方法**不要** `.Result` / `.Wait()` / `GetAwaiter().GetResult()`——除非在显式同步入口点（如 `Main`、`UseSyZero` 启动注册中已有的同步桥接）。
- 不要 `catch (Exception)` 后吞掉——至少记录日志并保留堆栈。
- 业务异常使用 [SyZero 框架内统一异常类型](../../src/SyZero.Core) 而非直接 `throw new Exception(...)`。

## 可空性

- 框架模块（`src/SyZero.Core/**`）**未启用** `<Nullable>`——遵循目标项目设置，不要单文件加 `#nullable enable`。
- 测试项目（`src/SyZero.Tests`）启用 `Nullable` —— 新代码须正确标注 `?`，不要用 `!` 抑制空检查除非有明确理由。

## 依赖注入

- 实现 `IScopedDependency` / `ISingletonDependency` / `ITransientDependency` 自动注册，**不要**再手动 `services.AddScoped(...)` 同一类型。
- 通过构造函数注入；**不要**使用 `SyZeroUtil.ServiceProvider.GetService<T>()` 作为常规依赖获取手段（仅在框架内部桥接场景使用）。

## 注释与提交

- 代码注释、`<summary>`、commit message 默认使用**简体中文**。
- TODO/FIXME 标注作者或追踪 issue：`// TODO(haitao): ...`。

## 不要做

- 不要 `var` 后跟显式类型字面量（`var i = (int)x;` 形式）—— 直接 `int i = (int)x;`。
- 不要在 `*.cs` 内硬编码连接串、密钥；走 `IConfiguration` / `IOptions<T>`。
- 不要重新发明已有抽象（`IRepository<T>` / `IEventBus` / `IServiceManagement`），优先复用核心包契约。
