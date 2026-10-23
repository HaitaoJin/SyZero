---
description: 创建一个新的 SyZero 框架模块（含项目、文档 README、测试引用、中央版本登记）
mode: agent
---

# 新建 SyZero 框架模块

按需收集信息后，在仓库内一次性创建一个**可发布**的新框架模块。

## 输入

- `<Module>`：模块短名（不含 `SyZero.` 前缀），例如 `Kafka`、`MinIO`、`Quartz`。如未提供，主动询问用户。
- 可选：模块一句话描述、依赖的 NuGet 包及版本。

## 步骤

1. 校验：`src/SyZero.Module/SyZero.<Module>/` 不存在；`docs/modules/SyZero.<Module>.md` 不存在。
2. 创建项目 [src/SyZero.Module/SyZero.\<Module\>/SyZero.\<Module\>.csproj](../../src/SyZero.Module)：
   - `<TargetFramework>net10.0</TargetFramework>`
   - `<IsPackable>true</IsPackable>`
   - 描述 / 标签
   - 引用核心 `SyZero.Core` 项目：`<ProjectReference Include="..\..\SyZero.Core\SyZero.Core.csproj" />`
   - **不要**在 `PackageReference` 上写 `Version`
3. 创建 `SyZero<Module>Extension.cs`，提供 `AddSyZero<Module>(this IServiceCollection, ...)` 与（必要时）`UseSyZero<Module>(this IApplicationBuilder)`。命名/风格参考 [src/SyZero.Core/SyZeroExtension.cs](../../src/SyZero.Core/SyZeroExtension.cs) 与既有模块。
4. 若引入新依赖：在 [Directory.Packages.props](../../Directory.Packages.props) 添加 `<PackageVersion Include="..." Version="..." />`，然后在 csproj 中 `<PackageReference Include="..." />`（无版本）。
5. 创建 NuGet README：[docs/modules/SyZero.\<Module\>.md](../../docs/modules/MODULE_TEMPLATE.md)，参考 [MODULE_TEMPLATE.md](../../docs/modules/MODULE_TEMPLATE.md)。
6. 在 [src/SyZero.Tests/SyZero.Tests.csproj](../../src/SyZero.Tests/SyZero.Tests.csproj) 增加 `<ProjectReference>`；创建 `src/SyZero.Tests/<Module>Tests.cs`，至少包含一条占位测试（标 `[Fact(Skip="...")]` 也可），遵循 [tests.instructions.md](../instructions/tests.instructions.md)。
7. 在 [SyZero.slnx](../../SyZero.slnx) 中加入新项目。
8. 运行 `dotnet build SyZero.slnx -c Release` 验证；如失败，定位并修复。
9. 给出汇总（创建/修改的文件列表）。

## 完成标准

- `dotnet build SyZero.slnx -c Release` 成功。
- 新模块出现在解决方案中，并被测试项目引用。
- `docs/modules/SyZero.<Module>.md` 存在（否则发布 NuGet 时缺 README）。

## 不要做

- 不要修改 [Directory.Build.props](../../Directory.Build.props) 的 `<Version>`。
- 不要执行 `nuget/批量生成nuget.bat` 或 `nuget/推送nuget.bat`（发布是独立动作，见 release-nuget 提示）。
