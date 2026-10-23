# 服务注册与发现

SyZero 通过统一的 `IServiceManagement` 抽象管理服务注册、发现、健康检查和部分 Leader 选举能力。你可以先用内置轻量实现启动，再按环境切换到 Redis、Consul 或 Nacos。

## 后端选择

| 后端 | 注册方式 | 适用场景 |
| --- | --- | --- |
| Local | `AddLocalServiceManagement(builder.Configuration)` | 单机开发、本地联调 |
| DB | `AddDBServiceManagement(builder.Configuration)` | 简单多实例部署，希望基于数据库保存注册信息 |
| Redis | `AddSyZeroRedis(builder.Configuration)` + `AddRedisServiceManagement(builder.Configuration)` | 轻量分布式部署，需要 Pub/Sub 通知 |
| Consul | `AddConsul(builder.Configuration)` | 标准生产环境服务发现 |
| Nacos | `AddNacos(builder.Configuration)` | 已使用 Nacos 作为注册中心或配置中心 |

## 1. 所有模式的共同前提

无论选择哪种实现，都建议保留下面的启动骨架：

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddSyZero();

var app = builder.Build();

app.UseSyZero();
app.Run();
```

`builder.AddSyZero()` 会注册框架核心能力，`app.UseSyZero()` 会配合 `SyZeroBootstrapper` 在应用生命周期内执行服务注册与注销。

## 2. 本地开发：LocalServiceManagement

`LocalServiceManagement` 会把 `localhost`、回环地址以及本机网卡地址视为同一个本地实例，并统一通过 `localhost` 访问，避免 Docker/WSL 虚拟网卡产生重复注册和不可达的健康检查。

这是最轻量的方式，适合本机多服务联调。

```csharp
builder.Services.AddLocalServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = false;
}, builder.Configuration);
```

如果你只是想先把 Feign 或网关跑起来，本地模式通常已经足够。

也可以在配置中预置本地实例，适合让网关在启动时立即知道下游地址：

```json
{
  "LocalServiceManagement": {
    "EnableHealthCheck": true,
    "HealthCheckIntervalSeconds": 10,
    "Services": [
      {
        "ServiceName": "SyZero.Example1",
        "ServiceAddress": "localhost",
        "ServicePort": 5002,
        "ServiceProtocol": "HTTP",
        "HealthCheckUrl": "http://localhost:5002/health"
      }
    ]
  }
}
```

默认注册数据保存在系统临时目录的 `syzero_services.json`，并通过文件监听在本机进程间同步。生产环境需要跨机器一致性时，应改用 Redis、Consul 或 Nacos。

## 3. 基于数据库：DBServiceManagement

当你暂时不想引入 Consul / Nacos，但又需要多实例共享注册信息时，可以使用数据库实现。

```csharp
builder.Services.AddDBServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = true;
}, builder.Configuration);
```

这条路线适合：

1. 中小规模部署
2. 现有环境里数据库最稳定、最容易拿到
3. 希望把注册信息和业务运维放在同一基础设施层

## 4. 基于 Redis：RedisServiceManagement

Redis 路线比 DB 更适合分布式环境，也能利用 Pub/Sub 传播服务变更。

```csharp
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddRedisServiceManagement(builder.Configuration, options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = true;
    options.EnablePubSub = true;
});
```

配置示例：

```json
{
  "Redis": {
    "Type": "MasterSlave",
    "Master": "localhost:6379,password=123456,defaultDatabase=0",
    "Slave": []
  },
  "RedisServiceManagement": {
    "EnableHealthCheck": true,
    "EnableLeaderElection": true,
    "EnablePubSub": true
  }
}
```

## 5. 生产环境：Consul

如果你的服务治理已经基于 Consul，直接接入它最自然。

```csharp
builder.Services.AddConsul(builder.Configuration);
```

配置示例：

```json
{
  "Consul": {
    "ConsulAddress": "http://localhost:8500"
  }
}
```

如果需要在绑定配置后再覆写参数，也可以使用委托重载。

## 6. 生产环境：Nacos

如果你的平台使用 Nacos 作为注册中心或配置中心，可直接启用：

```csharp
builder.Services.AddNacos(builder.Configuration);
```

配置示例：

```json
{
  "Nacos": {
    "ServerAddresses": [ "http://localhost:8848" ],
    "Namespace": "public"
  }
}
```

Nacos 方案尤其适合已经依赖其配置中心能力的团队。

## 7. 与 Feign / 网关的关系

服务发现本身只是后端注册中心。真正消费它的通常是下面两类模块：

1. [`SyZero.Feign`](/modules/SyZero.Feign) 这类远程调用模块
2. [`SyZero.Gateway.Ocelot`](/modules/SyZero.Gateway.Ocelot) / [`SyZero.Gateway.YARP`](/modules/SyZero.Gateway.YARP) 这类网关模块

因此常见的组合是：

```csharp
builder.AddSyZero();
builder.Services.AddLocalServiceManagement(builder.Configuration);
builder.Services.AddSyZeroFeign(builder.Configuration);
```

或者：

```csharp
builder.AddSyZero();
builder.Services.AddConsul(builder.Configuration);
builder.Services.AddSyZeroFeign(builder.Configuration);
```

## 8. 选择建议

1. 本地开发先用 `AddLocalServiceManagement(builder.Configuration)`
2. 简单分布式环境可用 `AddRedisServiceManagement(builder.Configuration)` 或 `AddDBServiceManagement(builder.Configuration)`
3. 标准生产环境优先选择 `AddConsul(builder.Configuration)` 或 `AddNacos(builder.Configuration)`
4. 如果你只跑单体宿主，例如 `Example3.SPA`，通常不需要额外的服务发现组件

## 9. 本地服务发现排障

### 健康检查重复或持续失败

先在 Aspire 跟踪详情中查看失败的出站地址。如果地址是 Docker/WSL 虚拟网卡，例如 `172.x.x.x:5002`，确认使用 v10 最新实现并重启全部服务。`LocalServiceManagement` 会将本机地址归一化为 `localhost`，同一协议和端口只保留一个逻辑实例。

### Feign 报告找不到健康实例

按顺序检查：

1. 调用方和被调用方使用完全相同的 `ServiceName`。
2. `HealthCheckUrl` 可以从调用方进程访问并返回 2xx。
3. `SyZero:Port` 与服务实际监听端口一致。
4. 调用方已经注册 `AddLocalServiceManagement(builder.Configuration)` 和 `AddSyZeroFeign(builder.Configuration)`。
5. 修改配置后已重启进程，且查看的是新的跟踪记录。

### 何时不应使用 LocalServiceManagement

它面向同一台开发机或单机部署。容器跨主机、Kubernetes 或多节点生产环境不应依赖本地临时文件共享注册状态，应选择 Redis、Consul 或 Nacos。

## 下一步

1. 继续阅读 [事件总线与集成事件](/guide/event-bus)
2. 如果需要声明式远程调用，查看 [SyZero.Feign](/modules/SyZero.Feign)
3. 如果需要网关，查看 [SyZero.Gateway.Ocelot](/modules/SyZero.Gateway.Ocelot) 和 [SyZero.Gateway.YARP](/modules/SyZero.Gateway.YARP)
4. 使用 Aspire 联调时，查看 [AppHost 本地编排与可观测性](/guide/apphost-observability)
