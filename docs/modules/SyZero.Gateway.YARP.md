# SyZero.Gateway.YARP

SyZero 网关抽象（`SyZero.Gateway` 命名空间，位于 `SyZero.Core`）的 **YARP 实现**：基于 [Yarp.ReverseProxy](https://microsoft.github.io/reverse-proxy/) 提供高性能反向代理能力，路由 schema 与 SyZero.Gateway.Ocelot 共享 `configuration.json`，可平滑互换。

> 📐 同一份 `configuration.json` 在 SyZero.Gateway.Ocelot 与 SyZero.Gateway.YARP 之间可直接切换，业务代码无感。本模块由内部 `SyZeroYarpProxyConfigProvider`（`IProxyConfigProvider` 实现）承载内存型路由表，写入后自动通过 `IChangeToken` 触发 YARP 热更新，无需重启进程。

## 📦 安装

```bash
dotnet add package SyZero.Gateway.YARP
```

## ✨ 特性

- 🚀 **高性能反向代理** - 基于 Microsoft 官方 YARP，零拷贝/HTTP/3 友好
- 🔄 **运行时热更新** - 修改 `configuration.json` → 自动重建 YARP 路由表
- 🔁 **配置 schema 与 Ocelot 共享** - `Routes` / `UpstreamPathTemplate` / `DownstreamPathTemplate` / `ServiceName` 等字段语义一致
- ⚖️ **负载均衡映射** - SyZero 中性 `RoundRobin` / `LeastConnection` / `NoLoadBalance` → YARP 内置策略

---

## 🚀 快速开始

### 1. 准备 `configuration.json`（与 Ocelot 同 schema）

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/user-service/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST" ],
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamScheme": "http",
      "ServiceName": "user-service",
      "LoadBalancerOptions": { "Type": "RoundRobin" }
    }
  ]
}
```

### 2. 注册并映射

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddSyZero();

builder.Services.AddSyZeroYarp(opt =>
{
    opt.ConfigurationFileName = "configuration.json"; // 默认值
    opt.LoadFromConfigurationFile = true;
}, builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();
app.UseSyZero();

app.UseRouting();
app.UseCors("SyZero.Gateway.YARP");
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapSyZeroYarp(); // 注册 YARP 终端 + 装载 configuration.json
});

app.Run();
```

### 3. 运行时改路由

```csharp
// 通过 IGatewayConfigurationStore 写入并热下发
var store = app.Services.GetRequiredService<IGatewayConfigurationStore>();
var dto  = await store.ReadAsync();
dto.Routes.Add(new GatewayRoute
{
    UpstreamPathTemplate = "/order/{everything}",
    UpstreamHttpMethod = new() { "GET" },
    DownstreamPathTemplate = "/api/{everything}",
    ServiceName = "order-service"
});
await store.WriteAsync(dto); // 自动备份 + 热加载
```

---

## 📖 配置选项

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `CorsPolicyName` | `string` | `"SyZero.Gateway.YARP"` | CORS 策略名 |
| `ConfigurationFileName` | `string` | `"configuration.json"` | 内容根目录下的配置文件 |
| `LoadFromConfigurationFile` | `bool` | `true` | 启动时是否加载文件路由 |
| `LoadFromReverseProxySection` | `bool` | `false` | 是否额外从 `appsettings.json` 的 `ReverseProxy` 段加载 YARP 原生配置 |
| `AllowedOrigins` | `string[]` | 由 `SyZero:CorsOrigins` 解析 | CORS 白名单 |
| `EnableSwagger` / `EnableSwaggerGen` | `bool` | `false` | 默认关闭（YARP 不内建 Swagger 聚合） |

---

## 🔁 与 SyZero.Gateway.Ocelot 的对照

| 能力 | SyZero.Gateway.Ocelot | SyZero.Gateway.YARP |
|---|---|---|
| 路由 / 负载均衡 | ✅ | ✅ |
| Consul 服务发现 | ✅（内置 `AddConsul`） | ⚠️ 不内置，需要外部配合 `IDestinationResolver` |
| Polly 熔断 | ✅ | ⚠️ 通过 YARP `HttpClient` 配置 |
| Swagger 聚合 | ✅（MMLib.SwaggerForOcelot） | ❌ 不支持 |
| 性能 | 中 | 高（基于 Kestrel + 反向代理优化） |
| 动态热更新 | ✅（写文件 + `IFileConfigurationSetter`） | ✅（`IChangeToken`） |
| 与 SyZero.Gateway.Admin 兼容 | ✅ | ✅（共享 `IGatewayConfigurationStore`） |

---

## 🌐 服务发现集成（Consul / Nacos / Redis / Local / DB）

`SyZero.Gateway.YARP` 自带一个 **基于 `IServiceManagement` 的 `IDestinationResolver`**，
能把任意 SyZero 服务发现后端（Consul / Nacos / Redis / Local / DB）一行接入 YARP。

### 启用

```csharp
builder.Services.AddSyZeroYarp(builder.Configuration);
builder.Services.AddConsul(builder.Configuration);     // 或 AddNacos / AddRedisServiceManagement / AddLocalServiceManagement / AddDBServiceManagement
builder.Services.AddSyZeroYarpServiceDiscovery();      // ★ 关键：注册 IDestinationResolver
```

### 工作原理

1. `YarpConfigMapper` 在遇到带 `ServiceName` 的路由时，会为对应集群生成一个占位 destination：
   ```
   discovery://{ServiceName}/
   ```
2. YARP 加载/刷新该集群时调用 `SyZeroYarpServiceDiscoveryDestinationResolver`：
   - 识别 `discovery://` scheme，从 `IServiceManagement.GetHealthyServices(ServiceName)` 拉取健康实例。
   - 把 `(scheme, address, port)` 拼成真实 destination（按 `ServiceProtocol` 决定 `http` / `https`，gRPC 走 H2 over HTTP）。
3. 非 `discovery://` 的 destination 原样透传，可与静态集群混用。

### 实例上下线即时反馈

Resolver 在首次解析每个服务时会自动向 `IServiceManagement.Subscribe(...)` 注册回调，并通过 `IChangeToken` 通知 YARP；当 Consul / Nacos 等后端推送服务变更时，YARP 会**仅刷新对应 cluster** 的目标列表，无需重新加载整份网关配置文件，秒级生效。

### 注意事项

- gRPC 默认使用 `http` scheme（YARP 通过 H2 透传）。如需 `https`，请把 `ServiceProtocol` 设为 `HTTPS`。
- Resolver 实现了 `IDisposable`，应用退出时会自动 `Unsubscribe` 已订阅的服务名，避免回调泄漏。

---

## ⚖️ 负载均衡与健康检查

### 跨后端 LB 策略映射

`GatewayLoadBalancerOptions.Type`（中性 schema，命名沿用 ocelot 习惯）由 `YarpConfigMapper` 自动翻译为 YARP 原生策略，**同一份 `configuration.json` 在 YARP / Ocelot 后端表现一致**：

| `GatewayLoadBalancerOptions.Type` | YARP `LoadBalancingPolicy` | Ocelot `LoadBalancerOptions.Type` |
|---|---|---|
| `RoundRobin`（默认） | `RoundRobin` | `RoundRobin` |
| `LeastConnection` | `LeastRequests` | `LeastConnection` |
| `NoLoadBalance` | `FirstAlphabetical`（始终选第一个） | `NoLoadBalancer` |
| 其他/null | YARP 默认（`PowerOfTwoChoices`） | Ocelot 默认 |

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/user-service/{everything}",
      "DownstreamPathTemplate": "/api/{everything}",
      "ServiceName": "user-service",
      "LoadBalancerOptions": { "Type": "LeastConnection" }
    }
  ]
}
```

> 测试验证见 [src/SyZero.Tests/YarpTests.cs](https://github.com/syzero/syzero/tree/main/src/SyZero.Tests/YarpTests.cs) 中 `YarpConfigurationApplier_MapsLoadBalancingPolicy`。

### 健康检查

SyZero 的健康过滤集中在 **服务发现源头**：`IServiceManagement.GetHealthyServices(...)` 返回的实例都已通过 Consul / Nacos / Redis 等后端的健康判定。因此本模块**默认不启用 YARP 自身的 active health check**：

- 优势：单一事实来源，避免 YARP 与服务注册中心两套健康判定打架。
- 实例上下线由 `IServiceManagement.Subscribe(...)` 推送，秒级反馈给 YARP（见上文「实例上下线即时反馈」）。
- 如确需 YARP active probe，可在 `appsettings.json` 的 `ReverseProxy:Clusters:<id>:HealthCheck` 段直接配置，并把 `LoadFromReverseProxySection` 设为 `true` 与服务发现共存。

---

## ⚠️ 注意事项

1. **集群目标解析** —— SyZero 中性路由只携带 `ServiceName`，本模块按 `ServiceName` 聚合 cluster；推荐启用 [服务发现集成](#-服务发现集成-consul--nacos--redis--local--db) 由 `IServiceManagement` 自动展开真实实例；也可在 `appsettings.json` 的 `ReverseProxy` 段补齐 `Clusters[*].Destinations`，并将 `LoadFromReverseProxySection` 设为 `true`。2. **catch-all 模板** —— Ocelot 的 `{everything}` 会自动转写为 YARP 风格的 `{**catchAll}`；其余 `{id}` 单段占位符语义一致，无需改动。
3. **Swagger 聚合** —— YARP 没有等价于 `MMLib.SwaggerForOcelot` 的中间件；如需聚合下游 Swagger 请改用 `SyZero.Gateway.Ocelot` 或自行实现。
4. **配置注释** —— 与 Ocelot 实现一致，`System.Text.Json` 写回时不保留 `//` 注释（备份保留原文）。

---

## 📄 许可证

MIT License - 详见 [LICENSE](../../LICENSE)
