---
description: 在 src/SyZero.Service/ 下创建一个新业务服务（Web/Application/IApplication/Core 四层）
mode: agent
---

# 新建业务服务

按 [SyZero.Example1.Service](../../src/SyZero.Service/SyZero.Example1.Service) 的结构脚手架一个新业务服务。

## 输入

- `<Biz>`：业务服务短名（`PascalCase`），如 `Order`、`Product`、`Account`。如未给出，主动询问。
- 选择数据访问后端：**EF Core / SqlSugar / MongoDB**（默认 SqlSugar，与 Example1 一致；用户偏好 SQLite 时优先选 EF Core + Sqlite）。
- 是否启用：Swagger / 服务注册（Consul/Nacos/Local）/ Redis 缓存 / 事件总线。

## 步骤

1. **校验**：`src/SyZero.Service/SyZero.<Biz>.Service/` 不存在。
2. **创建解决方案目录**与四个项目（全部 `net9.0`，**不写包版本**）：

   | 项目 | 引用 | 关键内容 |
   |---|---|---|
   | `SyZero.<Biz>.Core` | （仅核心 SyZero） | `Entities/` 实体、`DbContext/` 或 `Repository/`、领域服务 |
   | `SyZero.<Biz>.IApplication` | `Core` | `I<X>Service : IApplicationService` 接口、DTO |
   | `SyZero.<Biz>.Application` | `IApplication` + `SyZero.AutoMapper` + 选定数据访问模块 | `<X>Service : SyZeroServiceBase, I<X>Service, IScopedDependency`、`MapProfile/<Entity>Profile.cs` |
   | `SyZero.<Biz>.Web` | `Application` + `SyZero.AspNetCore` + `SyZero.DynamicWebApi` + `SyZero.Swagger` | `Program.cs`、`Controllers/`、`appsettings.json`（含 `SyZero` 节点）、`Dockerfile`、`log4net.config` |

3. **`Program.cs` 模板**（最小可用）：
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   builder.AddSyZero();
   builder.Services.AddControllers();
   builder.Services.AddDynamicWebApi(new DynamicWebApiOptions {
       DefaultApiPrefix = "/api",
       DefaultAreaName = "<Biz>"
   });
   builder.Services.AddSwagger();
   builder.Services.AddSyZeroAutoMapper();
   // 数据访问后端（按选择二选一，不要同时启用多个 IRepository<T> 实现）
   builder.Services.AddSyZeroSqlSugar();   // 或 AddSyZeroEntityFrameworkCore() / AddSyZeroMongoDB()

   var app = builder.Build();
   app.UseSyZero();
   app.UseSwagger();
   app.UseSwaggerUI();
   app.MapControllers();
   app.Run();
   ```

4. **`appsettings.json`** 必须含 `SyZero` 节点（`Name="<Biz>"` / `Protocol` / `Port` / `Ip` / `WanIp`），参考 [README.md](../../README.md) 配置示例。
5. **解决方案注册**：在 [SyZero.slnx](../../SyZero.slnx) 中加入 4 个新项目；可选地创建 `SyZero.<Biz>.sln`（与 Example1 一样有独立 sln 便于子模块开发）。
6. **构建验证**：`dotnet build SyZero.slnx -c Release`；如失败，定位并修复。
7. **汇总**输出创建的文件清单与启动命令（`dotnet run --project src/SyZero.Service/SyZero.<Biz>.Service/SyZero.<Biz>.Web`）。

## 完成标准

- 四层项目均存在且依赖方向正确（`Web → Application → IApplication → Core`，**严格单向**）。
- `dotnet build SyZero.slnx -c Release` 通过。
- 无 `PackageReference` 写死版本号。
- 至少一个示例 `I<X>Service` + `<X>Service` + `<X>Controller`（或仅依赖 `DynamicWebApi` 自动暴露），可调通 `GET /api/<biz>/<x>/...`。

## 不要做

- 不要把领域逻辑写进 `Web` 控制器；不要让 `IApplication` / `Core` 引用 EF/SqlSugar/MongoDB 客户端类型。
- 不要为业务服务在 [Directory.Build.props](../../Directory.Build.props) 设 `<IsPackable>true</IsPackable>` —— 业务服务不发布 NuGet。
- 不要为单业务服务建独立 `Tests` 项目——业务服务测试可放在 `src/SyZero.Tests/<Biz>ServiceTests.cs`，或在该业务下新增 `Tests/` 目录但**不要**污染框架测试项目。
