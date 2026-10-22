# 架构与约定

SyZero 是一个轻量级、模块化的 .NET 微服务开发框架。它不会把所有能力塞进一个大包，而是把核心抽象放在 `SyZero.Core`，把具体实现拆成多个可插拔 NuGet 模块。

## 设计目标

1. **按需组合**：只引用当前服务需要的模块，避免无关依赖进入业务项目
2. **抽象稳定**：仓储、事件总线、服务治理、远程调用等能力优先面向统一接口编程
3. **实现可替换**：同一能力可以有本地、数据库、Redis、Consul、Nacos、RabbitMQ、Kafka 等不同后端
4. **业务分层清晰**：业务契约、应用编排、领域模型和宿主基础设施保持明确边界
5. **开发体验轻量**：开发和中小规模场景可先使用内置实现，生产环境再替换为专业中间件模块

## 包与模块边界

SyZero 的模块边界大致分为两类：

| 类型 | 位置 | 说明 |
| --- | --- | --- |
| 核心抽象 | `SyZero.Core` | 依赖注入、应用服务、仓储/工作单元抽象、服务管理抽象、事件总线抽象、轻量内置能力 |
| 可插拔实现 | `SyZero.Module/SyZero.*` | ASP.NET Core、DynamicWebApi、EF Core、SqlSugar、MongoDB、Redis、Consul、Nacos、RabbitMQ、Kafka、网关、可观测性等 |

新增业务项目时，通常先引用：

```bash
dotnet add package SyZero.Core
dotnet add package SyZero.AspNetCore
```

再按场景追加模块：

```bash
dotnet add package SyZero.DynamicWebApi
dotnet add package SyZero.Swagger
dotnet add package SyZero.SqlSugar
dotnet add package SyZero.Redis
```

## 推荐分层

对于一个业务服务，推荐采用下面的四层结构：

```text
MyCompany.OrderService/
├─ MyCompany.Order.Web/           # ASP.NET Core 宿主、控制器、中间件、Swagger
├─ MyCompany.Order.Application/   # 应用服务实现、用例编排、事务边界
├─ MyCompany.Order.IApplication/  # 应用服务接口、DTO、远程调用契约
└─ MyCompany.Order.Core/          # 实体、值对象、领域服务、仓储扩展
```

依赖方向建议固定为：

1. `Web` 引用 `Application` 和 `IApplication`
2. `Application` 引用 `IApplication` 和 `Core`
3. `IApplication` 只放契约和 DTO，不引用 `Web`
4. `Core` 放领域模型和基础业务对象，不反向依赖上层

这样做的好处是：动态 Web API、Feign、gRPC 和 SDK 都可以围绕 `IApplication` 契约展开，数据库和中间件替换也不会直接污染接口层。

## 启动流程

一个典型 SyZero Web 服务启动流程如下：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

builder.Services.AddControllers();
builder.Services.AddDynamicWebApi();
builder.Services.AddSwagger();

var app = builder.Build();

app.UseSyZero();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI();

app.Run();
```

关键步骤说明：

| 步骤 | 作用 |
| --- | --- |
| `builder.AddSyZero()` | 绑定全局配置，注册核心服务、生命周期标记类型、校验、缓存和 `SyZeroBootstrapper` |
| `builder.Services.AddSyZero<Module>()` | 按模块注册具体能力，例如仓储、服务发现、日志、事件总线 |
| `app.UseSyZero()` | 设置运行期服务提供者，使 `SyZeroServiceBase` 等基础设施能获取作用域服务 |
| `SyZeroBootstrapper` | 作为 `IHostedService` 在启动/停止阶段异步执行服务注册与注销 |

## 依赖注入约定

SyZero 通过标记接口自动注册服务：

| 接口 | 生命周期 | 适用场景 |
| --- | --- | --- |
| `IScopedDependency` | Scoped | 应用服务、仓储封装、请求级业务组件 |
| `ITransientDependency` | Transient | 无状态轻量组件 |
| `ISingletonDependency` | Singleton | 全局状态、缓存协调器、配置类服务 |

示例：

```csharp
public interface IUserProfileService
{
    Task<UserProfileDto> GetAsync(long userId);
}

public class UserProfileService : IUserProfileService, IScopedDependency
{
    public Task<UserProfileDto> GetAsync(long userId)
    {
        // ...
    }
}
```

框架会扫描实现这些接口的非抽象类，并注册到它们实现的接口上。对业务代码来说，优先使用构造函数注入，不建议在业务方法里临时解析服务。

## 应用服务约定

应用服务是业务用例的入口。常见写法是：

```csharp
using SyZero.Application.Attributes;
using SyZero.Application.Service;

[DynamicApi]
public interface IOrderAppService : IApplicationService, IDynamicApi
{
    Task<OrderDto> GetAsync(long id);
    Task<long> CreateAsync(CreateOrderInput input);
}

public class OrderAppService : SyZeroServiceBase, IOrderAppService, IScopedDependency
{
    public Task<OrderDto> GetAsync(long id)
    {
        // 查询并返回 DTO
    }

    public Task<long> CreateAsync(CreateOrderInput input)
    {
        // 编排业务用例
    }
}
```

建议遵守下面几条规则：

1. 对外契约放在 `IApplication` 层，接口继承 `IApplicationService`
2. 需要动态发布成 API 的契约继承 `IDynamicApi` 并标记 `[DynamicApi]`
3. DTO 不直接复用数据库实体，避免把持久化模型暴露给调用方
4. 应用服务负责用例编排，复杂领域规则下沉到领域服务或实体方法
5. 实现类继承 `SyZeroServiceBase` 后，可使用 `ObjectMapper`、`UnitOfWork`、`Logger` 等基础能力

## 动态 API 约定

`SyZero.DynamicWebApi` 和 `SyZero.DynamicGrpc` 共用核心动态 API 筛选规则。一个类型要被动态发布，通常需要满足：

1. 实现 `IDynamicApi`
2. 类型或接口上存在 `[DynamicApi]`
3. 类型是公共、非抽象、非泛型类型
4. 没有被 `[NonDynamicApi]`、`[NonWebApiService]` 等排除

Web API 路由默认由三部分组成：

```text
/{DefaultApiPrefix}/{DefaultAreaName}/{ControllerName}/{ActionName}
```

例如：

```csharp
builder.Services.AddDynamicWebApi(new DynamicWebApiOptions
{
    DefaultApiPrefix = "/api",
    DefaultAreaName = "OrderService"
});
```

`OrderAppService.GetAsync` 默认会移除 `AppService` / `Service`、`Async` 等后缀，并按配置生成小写路由。需要明确控制 HTTP 方法或路径时，可以在接口方法上使用 `[Get]`、`[Post]`、`[Put]`、`[Delete]`。

## 配置约定

基础服务配置放在 `SyZero` 节点：

```json
{
  "SyZero": {
    "Name": "OrderService",
    "Protocol": "HTTP",
    "Port": 5001,
    "Ip": "",
    "WanIp": "",
    "InspectInterval": 5
  }
}
```

常用字段含义：

| 字段 | 说明 |
| --- | --- |
| `Name` | 服务名，用于服务注册、网关、远程调用和日志识别 |
| `Protocol` | 服务协议，常见为 `HTTP` / `HTTPS` / `GRPC` |
| `Port` | 服务监听端口 |
| `Ip` / `WanIp` | 内网 / 外网地址，服务发现或网关场景会用到 |
| `InspectInterval` | 健康检查间隔，具体后端可按需使用 |

模块配置独立放在自己的节点，例如 `ConnectionString`、`Redis`、`Consul`、`Nacos`、`Feign`、`RabbitMQ`。不要把所有模块配置混在一个自定义节点里，这样后续替换模块时更容易迁移。

## 多后端抽象

SyZero 很多能力都遵循“核心接口 + 多个后端实现”的模式：

| 能力 | 核心抽象 | 常见实现 |
| --- | --- | --- |
| 服务管理 | `IServiceManagement` | Local / DB / Redis / Consul / Nacos |
| 事件总线 | `IEventBus` | Local / DB / Redis / RabbitMQ / Kafka |
| 仓储 | `IRepository<T>` / `IUnitOfWork` | EF Core / SqlSugar / MongoDB |
| 远程调用 | `IRemoteServiceProvider` | Feign HTTP provider |

业务代码应尽量依赖核心抽象，而不是直接依赖具体后端类型。这样可以先用本地或内存实现完成开发，再在生产环境切换到 Redis、Consul、RabbitMQ 等模块。

## 模块扩展方法约定

模块注册通常遵循下面的命名：

```csharp
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddSyZeroSqlSugar<MyDbContext>();
builder.Services.AddSyZeroFeign();
builder.Services.AddSyZeroOpenTelemetry();
```

中间件或终结点通常遵循：

```csharp
app.UseSyZeroDiagnostics();
app.MapSyZeroGraphQL();
```

新模块也建议保持这种命名风格，让用户可以从方法名直接判断模块作用和注册位置。

## 什么时候拆微服务

SyZero 支持单体、模块化单体和微服务，但不要求一开始就拆微服务。建议按下面顺序演进：

1. 先用清晰分层把业务边界划出来
2. 契约先放到 `IApplication`，DTO 和接口保持稳定
3. 需要跨进程调用时，再引入 Feign、服务发现、网关和事件总线
4. 数据一致性要求提高时，再引入 Outbox、分布式事务或消息队列

如果业务还在快速变化，优先保持模块化单体；当团队、部署、伸缩或故障隔离真的需要独立服务时，再拆成微服务。

## 下一步

1. 跟着 [快速开始](/guide/getting-started) 跑通最小服务
2. 阅读 [创建分层业务服务](/guide/layered-service) 建立标准业务结构
3. 阅读 [数据访问与仓储](/guide/data-access) 选择数据库 Provider
4. 阅读 [服务注册与发现](/guide/service-discovery) 接入服务治理
