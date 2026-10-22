# Example4.Monolith：分层单体服务示例

`SyZero.Example4.Monolith` 演示如何用推荐的 **Core / IApplication / Application / Web** 分层创建一个单体服务。它只有一个 Web 宿主和一个部署单元，但代码边界仍按业务服务建议拆分，适合小型后台、管理工具、内部系统或微服务拆分前的起步阶段。

> 源码位置：`src/SyZero.Service/SyZero.Example4.Monolith/`

## 项目结构

```
SyZero.Example4.Monolith/
├─ SyZero.Example4.Core/             # 领域模型、领域接口、进程内存储实现
├─ SyZero.Example4.IApplication/     # 应用服务接口 + DTO（DynamicWebApi 契约）
├─ SyZero.Example4.Application/      # 应用服务实现
├─ SyZero.Example4.Web/              # ASP.NET Core 宿主（端口 5005）
└─ app/                              # IApplication.dll 自动汇集目录
```

这个示例的“单体”指运行与部署形态：只有 `SyZero.Example4.Web` 一个进程对外提供 API，不接网关、服务发现或 Feign 远程调用。代码仍按推荐分层组织，依赖方向为 `Web -> Application -> IApplication + Core`。

## 技术栈

| 能力 | 使用方式 |
| --- | --- |
| Web 宿主 | ASP.NET Core 9 |
| SyZero 核心 | `builder.AddSyZero()` / `app.UseSyZero()` |
| API 暴露 | `SyZero.DynamicWebApi` 自动发布 `TodoAppService` |
| 文档 | `SyZero.Swagger`，默认路径 `/api/swagger` |
| 数据 | `InMemoryTodoStore` 进程内内存实现 |

## 启动示例

```pwsh
cd src/SyZero.Service/SyZero.Example4.Monolith/SyZero.Example4.Web
dotnet run
```

默认端口为 `5005`：

- `http://localhost:5005/`：服务信息
- `http://localhost:5005/api/swagger`：Swagger UI
- `GET http://localhost:5005/api/todo/list`：查看待办事项列表

## 关键代码

`SyZero.Example4.Web/Program.cs` 是单体服务的唯一启动入口：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();
builder.Services.AddControllers();
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = AppConfig.ServerOptions.Name
});
builder.Services.AddSwagger();

var app = builder.Build();

app.UseSyZero();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(options => options.RoutePrefix = "api/swagger");

app.Run();
```

`SyZero.Example4.Application` 中的应用服务实现 `IScopedDependency` 后会被 SyZero 自动注册：

```csharp
public class TodoAppService : SyZeroServiceBase, ITodoAppService, IScopedDependency
{
    private readonly ITodoStore _todoStore;

    public TodoAppService(ITodoStore todoStore)
    {
        _todoStore = todoStore;
    }
}
```

内存仓储实现 `ISingletonDependency`，因此同一个进程内的请求会共享待办事项数据：

```csharp
public class InMemoryTodoStore : ITodoStore, ISingletonDependency
{
}
```

## 适用场景

| 场景 | 建议 |
| --- | --- |
| 小型管理后台 | 直接使用单体项目，减少部署和服务治理成本 |
| 业务尚未稳定 | 先按推荐项目分层，等边界清晰后再拆服务 |
| 内部工具 | 进程内调用优先，不引入 Feign、网关、注册中心 |
| 需要真实数据库 | 将 `InMemoryTodoStore` 替换为 EF Core、SqlSugar 或 MongoDB 仓储 |

## 与其他示例的区别

| 示例 | 重点 |
| --- | --- |
| [Example1 + Example2 微服务互调示例](/guide/example1-example2-microservices) | 多服务、Feign 调用、服务管理 |
| [SyZero.Gateway 微服务网关示例](/guide/gateway-microservice-demo) | 网关路由、聚合 Swagger、网关管理 |
| [Example3.SPA 单体宿主示例](/guide/example3-spa) | React + ASP.NET Core 同源发布 |
| Example4.Monolith | 后端单体，一个 Web 宿主，代码按推荐分层 |

## 下一步

1. 接入 [数据访问与仓储](/guide/data-access)，把内存存储换成数据库。
2. 阅读 [创建分层业务服务](/guide/layered-service)，了解何时拆成独立 Core / IApplication / Application / Web 项目。
3. 如果业务需要拆服务，再参考 [Example1 + Example2 微服务互调示例](/guide/example1-example2-microservices)。
