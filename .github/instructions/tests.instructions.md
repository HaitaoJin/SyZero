---
description: SyZero 单一 xUnit 测试项目的约定
applyTo: "src/SyZero.Tests/**/*.cs,src/SyZero.Tests/**/*.csproj"
---

# 测试约定

适用范围：[src/SyZero.Tests/](../../src/SyZero.Tests)。这是仓库**唯一**的测试项目。

## 必须遵守

- **不要**新建独立的测试项目——所有模块测试都集中在 `SyZero.Tests`。
- 每个模块对应**一个**测试文件：`<Module>Tests.cs`（如 [RedisTests.cs](../../src/SyZero.Tests/RedisTests.cs)、[SqlSugarTests.cs](../../src/SyZero.Tests/SqlSugarTests.cs)）。
- 共享 fixture / 一次性初始化使用 xUnit `[Collection]` —— 复用现有定义：
  - [Log4NetCollection.cs](../../src/SyZero.Tests/Log4NetCollection.cs)
  - [AppConfigCollection.cs](../../src/SyZero.Tests/AppConfigCollection.cs)
  
  仅当确有新的全局初始化需求时才新增 `*Collection.cs`。
- 新增模块测试时，需在 [SyZero.Tests.csproj](../../src/SyZero.Tests/SyZero.Tests.csproj) 添加该模块的 `<ProjectReference>`。
- 此项目启用了 `<Nullable>enable</Nullable>` 与 `<ImplicitUsings>enable</ImplicitUsings>`，新代码须遵守可空性提示。

## 运行

```pwsh
dotnet test src/SyZero.Tests/SyZero.Tests.csproj
```

## 不要做

- 不要在 `PackageReference` 写 `Version`（中央版本管理，见 [Directory.Packages.props](../../Directory.Packages.props)）。例外：测试项目里现存的 `VersionOverride` 仅在确需覆盖时使用。
- 不要在测试中硬编码外部依赖端点（Redis/RabbitMQ/Consul/Nacos/MongoDB）——通过配置或 fixture 提供，必要时跳过未配置环境。
