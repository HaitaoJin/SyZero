# 快速开始

本页带你从一个空的 ASP.NET Core Web API 项目开始，启动一个最小 SyZero 服务，并通过动态 Web API 暴露一个应用服务。

## 前置条件

1. 安装 .NET 10 SDK
2. 准备一个可用的 NuGet 源
3. 本地端口 `5000` 未被占用

如果你已经有 ASP.NET Core 项目，可以跳过创建项目步骤，直接从安装 SyZero 包开始。

## 1. 创建 Web API 项目

```bash
dotnet new webapi -n MyService
cd MyService
```

## 2. 安装 NuGet 包

最小 Web API 宿主通常需要下面几个包：

```bash
dotnet add package SyZero.Core
dotnet add package SyZero.AspNetCore
dotnet add package SyZero.DynamicWebApi
dotnet add package SyZero.Swagger
```

如果后续需要数据库、缓存、服务发现或消息队列，再按需安装对应模块即可。SyZero 的模块都是独立 NuGet 包，不需要一次性引入全部能力。

## 3. 配置 Program.cs

将 `Program.cs` 调整为下面的结构：

```csharp
using SyZero;
using SyZero.DynamicWebApi;
using SyZero.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

builder.Services.AddControllers();
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = "MyService"
});
builder.Services.AddSyZeroOpenApi();

var app = builder.Build();

app.UseSyZero();
app.MapSyZeroOpenApi();
app.MapControllers();

app.Run();
```

这里有三个关键点：

1. `builder.AddSyZero()` 注册框架核心能力、自动依赖注入、基础缓存、校验和启动钩子
2. `AddDynamicWebApi(...)` 会把符合约定的应用服务发布成 Web API
3. `app.UseSyZero()` 设置框架运行期上下文，并配合宿主生命周期执行服务注册/注销

## 4. 添加基础配置

在 `appsettings.json` 中添加 `SyZero` 配置：

```json
{
  "SyZero": {
    "Name": "MyService",
    "Protocol": "HTTP",
    "Port": 5000,
    "Ip": "",
    "WanIp": ""
  },
  "AllowedHosts": "*"
}
```

`Name` 是当前服务名，后续会被服务注册、Feign 远程调用和网关管理等能力复用。`Protocol`、`Ip`、`Port` 用于描述服务实例地址；如果暂时不接服务发现，也建议先补齐这些字段。

## 5. 创建一个应用服务

添加 `HelloAppService.cs`：

```csharp
using SyZero;
using SyZero.Application.Attributes;
using SyZero.Application.Routing;
using SyZero.Application.Service;
using SyZero.Dependency;

[DynamicApi]
public interface IHelloAppService : IApplicationService, IDynamicApi
{
    [Get("ping")]
    Task<HelloOutput> PingAsync();

    [Post("echo")]
    Task<HelloOutput> EchoAsync(HelloInput input);
}

public class HelloAppService : SyZeroServiceBase, IHelloAppService, IScopedDependency
{
    public Task<HelloOutput> PingAsync()
    {
        return Task.FromResult(new HelloOutput("MyService", "pong"));
    }

    public Task<HelloOutput> EchoAsync(HelloInput input)
    {
        return Task.FromResult(new HelloOutput("MyService", input.Message));
    }
}

public record HelloInput(string Message);

public record HelloOutput(string Service, string Message);
```

这段代码里有几条 SyZero 约定：

1. 接口继承 `IApplicationService` 表示这是应用服务契约
2. 接口继承 `IDynamicApi` 并标记 `[DynamicApi]` 后，动态 API 模块会扫描并发布它
3. 实现类继承 `SyZeroServiceBase` 后，可以使用 `ObjectMapper`、`UnitOfWork`、`Logger` 等基础能力
4. 实现 `IScopedDependency` 后，类型会自动注册到依赖注入容器

## 6. 运行与验证

```bash
dotnet run --urls http://localhost:5000
```

打开 OpenAPI 文档：

```text
http://localhost:5000/openapi/v1.json
```

你应该能看到 `Hello` 相关接口。按上面的配置，常见生成路径会类似：

```text
GET  /api/myservice/hello/ping
POST /api/myservice/hello/echo
```

如果 Swagger 里没有看到接口，优先检查下面几项：

1. `AddDynamicWebApi(...)` 是否在 `AddControllers()` 之后调用
2. 应用服务接口是否继承了 `IDynamicApi`
3. 接口或实现类是否标记了 `[DynamicApi]`
4. 实现类是否可被 DI 注册，例如实现了 `IScopedDependency`

## 7. 接入更多能力

最小服务跑通后，可以按业务需要继续添加模块：

| 能力 | 常用模块 | 下一步 |
| --- | --- | --- |
| 分层业务服务 | `SyZero.Core` | [创建分层业务服务](/guide/layered-service) |
| 数据访问 | `SyZero.EntityFrameworkCore` / `SyZero.SqlSugar` / `SyZero.MongoDB` | [数据访问与仓储](/guide/data-access) |
| 服务注册与发现 | `SyZero.ServiceDiscovery.Consul` / `SyZero.ServiceDiscovery.Nacos` / `SyZero.Redis` | [服务注册与发现](/guide/service-discovery) |
| 事件总线 | `SyZero.EventBus.RabbitMQ` / `SyZero.EventBus.Kafka` / `SyZero.Redis` | [事件总线与集成事件](/guide/event-bus) |
| 网关 | `SyZero.Gateway.Ocelot` / `SyZero.Gateway.YARP` / `SyZero.Gateway.Admin` | [SyZero.Gateway 微服务网关示例](/guide/gateway-microservice-demo) |
| 本地编排与可观测性 | `SyZero.AppHost` / `SyZero.Observability.OpenTelemetry` | [AppHost 本地编排与可观测性](/guide/apphost-observability) |

## 常见问题

### 需要手动 AddScoped 吗

通常不需要。实现 `IScopedDependency`、`ISingletonDependency` 或 `ITransientDependency` 的类型会被自动注册。只有当你需要覆盖默认注册、绑定第三方类型或注册开放泛型时，才需要手动调用 `AddScoped` / `AddSingleton` / `AddTransient`。

### Dynamic Web API 会暴露所有类吗

不会。当前约定下，服务类型需要满足动态 API 筛选规则，例如实现 `IDynamicApi`、带有 `[DynamicApi]`，并且没有被 `[NonDynamicApi]` 或 `[NonWebApiService]` 排除。

### app.UseSyZero 必须调用吗

建议始终调用。它会设置 SyZero 的运行期服务提供者。服务注册/注销已经由 `SyZeroBootstrapper` 通过 `IHostedService` 异步处理，但 `UseSyZero()` 仍然是宿主接入 SyZero 的标准步骤。

## 延伸阅读

1. [架构与约定](/guide/architecture)
2. [创建分层业务服务](/guide/layered-service)
3. [数据访问与仓储](/guide/data-access)
4. [服务注册与发现](/guide/service-discovery)
5. [事件总线与集成事件](/guide/event-bus)
6. [AppHost 本地编排与可观测性](/guide/apphost-observability)
7. [模块总览](/modules/index)
