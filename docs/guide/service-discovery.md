# 服务注册与发现

SyZero 通过统一的 `IServiceManagement` 抽象管理服务注册、发现、健康检查和部分 Leader 选举能力。你可以先用内置轻量实现启动，再按环境切换到 Redis、Consul 或 Nacos。

## 后端选择

| 后端 | 注册方式 | 适用场景 |
| --- | --- | --- |
| Local | `AddLocalServiceManagement()` | 单机开发、本地联调 |
| DB | `AddDBServiceManagement()` | 简单多实例部署，希望基于数据库保存注册信息 |
| Redis | `AddSyZeroRedis()` + `AddRedisServiceManagement()` | 轻量分布式部署，需要 Pub/Sub 通知 |
| Consul | `AddConsul()` | 标准生产环境服务发现 |
| Nacos | `AddNacos()` | 已使用 Nacos 作为注册中心或配置中心 |

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

这是最轻量的方式，适合本机多服务联调。

```csharp
builder.Services.AddLocalServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = false;
});
```

如果你只是想先把 Feign 或网关跑起来，本地模式通常已经足够。

## 3. 基于数据库：DBServiceManagement

当你暂时不想引入 Consul / Nacos，但又需要多实例共享注册信息时，可以使用数据库实现。

```csharp
builder.Services.AddDBServiceManagement(options =>
{
    options.EnableHealthCheck = true;
    options.EnableLeaderElection = true;
});
```

这条路线适合：

1. 中小规模部署
2. 现有环境里数据库最稳定、最容易拿到
3. 希望把注册信息和业务运维放在同一基础设施层

## 4. 基于 Redis：RedisServiceManagement

Redis 路线比 DB 更适合分布式环境，也能利用 Pub/Sub 传播服务变更。

```csharp
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddRedisServiceManagement(options =>
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
builder.Services.AddConsul();
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
builder.Services.AddNacos();
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
builder.Services.AddLocalServiceManagement();
builder.Services.AddSyZeroFeign();
```

或者：

```csharp
builder.AddSyZero();
builder.Services.AddConsul();
builder.Services.AddSyZeroFeign();
```

## 8. 选择建议

1. 本地开发先用 `AddLocalServiceManagement()`
2. 简单分布式环境可用 `AddRedisServiceManagement()` 或 `AddDBServiceManagement()`
3. 标准生产环境优先选择 `AddConsul()` 或 `AddNacos()`
4. 如果你只跑单体宿主，例如 `Example3.SPA`，通常不需要额外的服务发现组件

## 下一步

1. 继续阅读 [事件总线与集成事件](/guide/event-bus)
2. 如果需要声明式远程调用，查看 [SyZero.Feign](/modules/SyZero.Feign)
3. 如果需要网关，查看 [SyZero.Gateway.Ocelot](/modules/SyZero.Gateway.Ocelot) 和 [SyZero.Gateway.YARP](/modules/SyZero.Gateway.YARP)
