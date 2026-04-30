# SyZero — Agent 指南

SyZero 是一个 **.NET 9** 模块化微服务框架，发布为一组 NuGet 包（`SyZero.*`，当前版本 `1.1.10`）。
面向用户的中文/英文文档：[README.md](README.md) · [README.en.md](README.en.md) · [docs/](docs/index.md)

> 与代理交互的默认语言为 **简体中文**。代码注释、提交说明、PR 描述也保持中文（除非用户另有说明）。

## 仓库布局

| 路径 | 用途 |
|---|---|
| [src/SyZero.Core/](src/SyZero.Core) | 框架内核 NuGet 包 `SyZero.Core`（`AssemblyName=SyZero`、根命名空间 `SyZero`），提供依赖注入、应用基础设施、仓储/工作单元抽象、轻量服务管理与事件总线 |
| [src/SyZero.Module/](src/SyZero.Module) | 框架可插拔模块包，每个子目录就是一个独立 NuGet 包（`SyZero.AspNetCore`、`SyZero.Redis`、`SyZero.Consul`、`SyZero.Nacos`、`SyZero.SqlSugar`、`SyZero.EntityFrameworkCore`、`SyZero.DynamicWebApi`、`SyZero.DynamicGrpc`、`SyZero.RabbitMQ`、`SyZero.OpenTelemetry`、`SyZero.ApiGateway`、`SyZero.Feign`、`SyZero.Swagger`、`SyZero.AutoMapper`、`SyZero.Log4Net`、`SyZero.MongoDB`、`SyZero.AspNetCore.SpaProxy`、`SyZero.Web.Common`） |
| [src/SyZero.Service/](src/SyZero.Service) | 示例业务服务（`SyZero.Example1.Service`、`SyZero.Example2.Service`），演示 **Web / Application / IApplication / Core** 分层，是新业务服务的参考模板 |
| [src/SyZero.Gateway/](src/SyZero.Gateway) | 基于 Ocelot 的 API 网关示例 |
| [src/SyZero.Tests/](src/SyZero.Tests) | 单一 xUnit 测试项目，引用并测试所有模块（按模块拆分文件，如 `RedisTests.cs`、`SqlSugarTests.cs`） |
| [docs/](docs/) | VitePress 文档站点（`docs/modules/*.md` 同时被打包为对应 NuGet 包的 README） |
| [nuget/](nuget/) | 输出目录与发布脚本（`批量生成nuget.bat` / `推送nuget.bat` / `刷新nuget缓存.bat`） |
| [SyZero.slnx](SyZero.slnx) | 解决方案文件（**新格式 .slnx**，不是 .sln） |

## 构建与测试

- 解决方案：`dotnet build SyZero.slnx -c Release`
- 测试：`dotnet test src/SyZero.Tests/SyZero.Tests.csproj`
- 打包全部模块：运行 [nuget/批量生成nuget.bat](nuget/批量生成nuget.bat)（输出到 `nuget/`）
- 清理 `bin`/`obj`/`*.pdb`：根目录 [Clear.bat](Clear.bat)
- 目标框架：`net9.0`（测试项目启用 `Nullable` 与 `ImplicitUsings`，但**框架模块默认未启用** `Nullable`——添加新代码时遵循目标项目设置）

## 关键约定（违反会破坏一致性）

### 1. 集中式包版本管理
- 所有 `PackageReference` **不要写 `Version`**——版本统一在 [Directory.Packages.props](Directory.Packages.props)（`ManagePackageVersionsCentrally=true`）。
- 新增依赖：先在 `Directory.Packages.props` 添加 `<PackageVersion .../>`，再在 csproj 中 `<PackageReference Include="..." />`。

### 2. 包元数据由 `Directory.Build.props` 统一注入
- [Directory.Build.props](Directory.Build.props) 自动注入 `Version`、`Authors`、`PackageIcon`、`LICENSE`、SourceLink、`PackageReadmeFile`（指向 `docs/modules/$(MSBuildProjectName).md`）。
- **新建框架模块**：在 `src/SyZero.Module/SyZero.<Name>/` 创建项目；同时在 `docs/modules/SyZero.<Name>.md` 创建 README——否则 `PackageReadmeFile` 引用会缺失（条件已使用 `Exists()` 保护，但发布时需要它）。
- 项目 csproj 设置 `<IsPackable>true</IsPackable>` 才会打包（默认 false）。

### 3. 依赖注入约定
- 实现 [IScopedDependency](src/SyZero.Core/Dependency/IScopedDependency.cs) / [ISingletonDependency](src/SyZero.Core/Dependency/ISingletonDependency.cs) / [ITransientDependency](src/SyZero.Core/Dependency/ITransientDependency.cs) 的类**自动注册**——**无需**手动 `AddScoped`。
- 业务服务继承 `SyZeroServiceBase`，业务接口继承 `IApplicationService`。

### 4. 模块扩展方法命名模式
- 注册：`builder.Services.AddSyZero<Module>(...)` 或 `builder.Add<Module>()`
- 中间件：`app.UseSyZero()` 由 [src/SyZero.Core/SyZeroExtension.cs](src/SyZero.Core/SyZeroExtension.cs) 提供，触发服务自动注册到 `IServiceManagement` 并在应用关闭时注销。
- 新模块应提供同样的 `Add*` / `Use*` 扩展，模式参考其他模块的 `*Extension.cs`。

### 5. 多后端抽象
许多核心能力提供**多后端实现**，统一 接口/工厂 模式（详见 [README.md](README.md) 中的对照表）：
- `IServiceManagement`：Local / DB / Redis / Consul / Nacos
- `IEventBus`：LocalEventBus / DBEventBus / RedisEventBus / RabbitMQEventBus
- 仓储 (`IRepository<T>`)：EntityFrameworkCore / SqlSugar / MongoDB

新增同类能力时**保持接口位于核心 `SyZero.Core` 包**，实现下沉到独立模块包。

### 6. 示例服务的分层（业务侧默认结构）
[src/SyZero.Service/SyZero.Example1.Service/](src/SyZero.Service/SyZero.Example1.Service) 演示推荐分层：
`SyZero.Example1.Web` → `SyZero.Example1.Application` → `SyZero.Example1.IApplication` → `SyZero.Example1.Core`
新业务服务请遵循此命名与依赖方向。

### 7. 测试
- 仅 **一个** xUnit 项目 [src/SyZero.Tests/](src/SyZero.Tests/)，按模块一个文件（`<Module>Tests.cs`）。
- 共享 fixture 通过 `[Collection]`（如 [Log4NetCollection.cs](src/SyZero.Tests/Log4NetCollection.cs)、[AppConfigCollection.cs](src/SyZero.Tests/AppConfigCollection.cs)）。新模块测试请沿用此模式而非新建项目。

## 容易踩坑

- **不要**在示例服务/网关里手动改 NuGet 版本——它们都走中央版本管理。
- 修改框架模块的公共 API（`*Extension.cs` 中的 `Add*`/`Use*` 签名）会影响下游使用者，请同步更新 `docs/modules/<Module>.md`。
- 修改 [Directory.Build.props](Directory.Build.props) 的 `Version` 会改变所有包版本——发布前与维护者确认。
- 解决方案文件是 `.slnx`（XML 新格式），部分旧版工具可能不识别——请用 .NET 9+ SDK / 最新 VS / VS Code C# Dev Kit。
- README 文档/中文标点遵循中文写作风格；示例代码块使用 ` ```csharp `。
