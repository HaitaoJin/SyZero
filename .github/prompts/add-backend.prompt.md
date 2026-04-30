---
description: 为 SyZero 多后端抽象（IServiceManagement / IEventBus / IRepository<T> 等）添加新实现的标准工作流
mode: agent
---

# 添加新后端实现

为 SyZero 现有抽象（`IServiceManagement` / `IEventBus` / `IRepository<T>` 或类似多后端契约）添加一个新的后端实现。**抽象保留在核心 `SyZero` 包，实现下沉到独立模块包。**

## 输入

- `<Abstraction>`：要扩展的抽象接口名（如 `IEventBus`、`IServiceManagement`、`IRepository<T>`）。如未给出，主动询问。
- `<Backend>`：新后端短名（如 `Kafka`、`Etcd`、`Postgres`）。如未给出，主动询问。
- 是否需要新增 NuGet 依赖（驱动客户端等）。

## 步骤

1. **定位抽象**：在 [src/SyZero.Core/](../../src/SyZero.Core) 下找到 `<Abstraction>` 接口（如 `IEventBus`、`IServiceManagement`），并阅读现有任意一个实现（如 `LocalEventBus` / `RedisEventBus` 在各自模块包中）以了解扩展点。
2. **决策包归属**：
   - 若 `<Backend>` 已有 SyZero 模块包（如 `SyZero.Redis`），实现放入该包。
   - 否则，按 [new-module 提示](new-module.prompt.md) 先创建 `SyZero.<Backend>` 模块包，再继续。
3. **实现接口**：
   - 类名 `<Backend><Abstraction去掉I前缀>`，如 `KafkaEventBus`、`EtcdServiceManagement`。
   - 标记合适的生命周期接口（通常 `ISingletonDependency`，**不要**手动 `AddSingleton`）。
   - 复用核心包的 DTO / 抽象类型，**不要**复制定义。
4. **注册扩展方法**：在该模块的 `<Module>Extension.cs` 添加 `AddSyZero<Backend><Abstraction>(this IServiceCollection, Action<Options>? configure = null)`。命名风格对齐既有方法（如 `AddRedisEventBus()` / `AddRabbitMQEventBus()`）。
5. **配置选项**：若需要选项类，命名 `<Backend><Abstraction>Options`，通过 `IOptions<T>` 注入；通过 `appsettings.json` 节点示例写入文档。
6. **依赖**：新增驱动包先在 [Directory.Packages.props](../../Directory.Packages.props) 登记 `<PackageVersion>`，再在 csproj 中无版本引用。
7. **测试**：在 [src/SyZero.Tests/](../../src/SyZero.Tests) 的对应 `<Module>Tests.cs` 中追加用例。外部依赖（broker/中间件）应通过配置/fixture 提供，不可硬编码端点；环境缺失时 `Skip`。
8. **文档**：更新 `docs/modules/SyZero.<Module>.md`（含 NuGet README）："快速开始"补 `AddSyZero<Backend><Abstraction>()` 示例和 `appsettings.json` 节点；同步更新 [README.md](../../README.md) 中相应的"对照表"行（如多后端实现表）。
9. **构建验证**：`dotnet build SyZero.slnx -c Release` 必须通过；运行 `dotnet test`（允许已 `Skip` 的环境性测试）。

## 完成标准

- 新实现可被 `builder.Services.AddSyZero<Backend><Abstraction>()` 注册。
- 抽象层（核心 `SyZero` 包）**未被修改**，或仅做向后兼容的扩展（新增成员而非破坏签名）。
- 模块 README 与根 README 的对照表已同步。
- 构建通过。

## 不要做

- 不要把后端特定类型（连接、客户端配置）暴露到核心 `SyZero` 包的公共 API。
- 不要修改既有实现的公共签名——新后端是**新增**而非替换。
- 不要在多个模块包中重复定义同一接口/DTO——共用类型必须在核心 `SyZero` 包。
- 不要为单个后端引入新的 `I<X>Factory` 抽象，除非至少有两个后端共享该工厂语义。
