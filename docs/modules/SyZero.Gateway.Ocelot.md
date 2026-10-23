# SyZero.Gateway.Ocelot

SyZero 网关抽象的 **Ocelot 25 实现**，提供动态路由、负载均衡、缓存、热更新与基于 `IServiceManagement` 的统一服务发现。

> 📐 抽象与实现分层：`SyZero.Core` 暴露 `IGateway` / `IGatewayConfigurationStore` / `IGatewayConfigurationApplier` / `GatewayConfiguration` 等抽象；本模块提供它们的 Ocelot 后端。后续可按相同模式新增其他网关实现包（如 YARP）。

## 📦 安装

```bash
dotnet add package SyZero.Gateway.Ocelot
```

## ✨ 特性

- 🚀 **服务路由** - 基于 Ocelot 的动态路由配置
- ⚖️ **负载均衡** - 支持多种负载均衡策略
- 🔎 **统一服务发现** - 支持 SyZero 的 Consul / Nacos / Redis / Local / DB 后端
- 🛡️ **HTTP 韧性** - 可组合 `SyZero.Resilience` 的标准重试、超时与熔断处理器

---

## 🚀 快速开始

### 1. 配置 ocelot.json

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5001 }
      ],
      "UpstreamPathTemplate": "/user-service/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ]
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

### 2. 注册服务

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.AddSyZero();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddSyZeroOcelot(options =>
{
    options.SwaggerTitle = "SyZero.Gateway";
}, builder.Configuration);
builder.Services.AddControllers();

var app = builder.Build();
app.UseSyZero();

app.UseRouting();
app.UseCors(SyZeroOcelotOptions.DefaultCorsPolicyName);
app.MapControllers();
await app.UseSyZeroOcelotAsync();
app.Run();
```

### 3. 使用示例

```csharp
// 网关自动转发请求，无需额外代码
// 客户端请求: GET http://gateway:5000/user-service/api/users/1
// 转发到: GET http://user-service:5001/api/users/1
```

---

## 📖 配置选项

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Routes` | `array` | `[]` | 路由配置数组 |
| `DownstreamPathTemplate` | `string` | `""` | 下游服务路径模板 |
| `UpstreamPathTemplate` | `string` | `""` | 上游请求路径模板 |
| `LoadBalancerOptions.Type` | `string` | `"RoundRobin"` | 负载均衡类型 |

---

## 📖 API 说明

### 路由配置

| 属性 | 说明 |
|------|------|
| `DownstreamHostAndPorts` | 下游服务地址列表 |
| `UpstreamHttpMethod` | 支持的 HTTP 方法 |
| `RateLimitOptions` | 限流配置 |
| `QoSOptions` | 服务质量配置 |

> 详细配置请参考 [Ocelot 文档](https://ocelot.readthedocs.io/)

---

## 🔧 高级用法

### Swagger 文档聚合

```csharp
builder.Services.AddSyZeroOcelot(options =>
{
    options.SwaggerTitle = "SyZero.Gateway";
    options.SwaggerGeneratorPath = "/swagger/docs";
}, builder.Configuration);

await app.UseSyZeroOcelotAsync();
```

### 自定义中间件

```csharp
builder.Services.AddOcelot()
    .AddDelegatingHandler<CustomHandler>();
```

---

## ⚠️ 注意事项

1. **配置文件** - 建议将网关路由单独放在 `ocelot.json` 或 `configuration.json`
2. **CORS** - `SyZero:CorsOrigins` 会自动注册命名 CORS 策略，未配置时默认退化为匿名跨域
3. **服务发现** - v10 仅保留 SyZero 统一 Provider，不再内置 Ocelot Consul Provider

---

## 🌐 统一服务发现（Consul / Nacos / Redis / Local / DB）

本模块提供 **基于 SyZero `IServiceManagement` 抽象的通用服务发现 Provider** —— `SyZeroDiscovery`。
凡是注册了 `IServiceManagement` 的后端（Consul / Nacos / Redis / Local / DB）都能直接接入 Ocelot，与 [SyZero.Gateway.YARP 的服务发现集成](/modules/SyZero.Gateway.YARP#-服务发现集成-consul--nacos--redis--local--db) 共用同一抽象。

### 启用

```csharp
builder.Services.AddNacos(builder.Configuration);   // 或 AddConsul / AddRedisServiceManagement / AddLocal/DBServiceManagement
builder.Services.AddSyZeroOcelot(opts =>
{
    opts.EnableSyZeroServiceDiscovery = true;       // ★ 启用 SyZeroDiscovery
}, builder.Configuration);
```

`ocelot.json` 中切到 `SyZeroDiscovery`：

```json
{
  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Type": "SyZeroDiscovery"
    }
  },
  "Routes": [
    {
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "ServiceName": "user-service",
      "UpstreamPathTemplate": "/user-service/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "LoadBalancerOptions": { "Type": "RoundRobin" }
    }
  ]
}
```

### 工作原理

1. `AddSyZeroOcelot` 在开启 `EnableSyZeroServiceDiscovery` 后向 DI 注册一个 `ServiceDiscoveryFinderDelegate`。
2. Ocelot 25 的 `ServiceDiscoveryProviderFactory` 在路由使用服务发现时调用该委托，得到 `SyZeroDiscovery` 实例（类的简单类型名必须等于 `Type` 字段）。
3. `SyZeroDiscovery.GetAsync()` 调 `IServiceManagement.GetHealthyServices(route.ServiceName)`，把 `ProtocolType` 映射成 `http` / `https`（gRPC 默认用 `http` 走 H2 透传）。

> 本 Provider **不绑定具体后端**，可在不同部署环境（Consul / Nacos / Redis / 本地）共用同一份 `ocelot.json`，并复用 SyZero 自身的健康状态能力。

### 负载均衡与健康检查

- LB 策略沿用 Ocelot 原生：`RoundRobin` / `LeastConnection` / `NoLoadBalancer` / `CookieStickySessions`，在路由 `LoadBalancerOptions.Type` 中配置即可，与 [SyZero.Gateway.YARP 的策略表](/modules/SyZero.Gateway.YARP#-负载均衡与健康检查) 一一对应。
- 健康过滤集中在 `IServiceManagement.GetHealthyServices(...)`，避免网关与注册中心产生两套健康判定。

---

## 🎛️ 网关管理页面（SyZero.Gateway 示例）

仓库中的 [SyZero.Gateway](https://github.com/syzero/syzero/tree/main/src/SyZero.Gateway) 示例自带一个轻量后台 `/admin`，用于在线维护 `configuration.json` 中的 `Routes` 与 `SwaggerEndPoints`。

### 启用

1. 在 `appsettings.json` 配置管理员凭据：
   ```json
   "Gateway": {
     "Admin": {
       "Username": "admin",
       "Password": "请改为强密码"
     }
   }
   ```
   未配置或 `Password` 为空时，访问 `/admin` 与 `/admin-api` 会返回 `503`，避免空密码暴露管理面。
2. 启动网关，浏览器访问 `http://<网关>/admin`，按浏览器弹框输入凭据。

### 功能

| 入口 | 说明 |
|---|---|
| `/admin`（概览） | 路由数、Swagger 端点数、配置文件路径、最近备份时间，「从磁盘重载」按钮强制重新读取并下发 Ocelot |
| `/admin/routes` | 对 `Routes` 增删改查（弹窗表单），保存时自动备份原文件并热加载到 Ocelot，无需重启 |
| `/admin/swagger-endpoints` | 维护通用网关配置中的文档端点元数据；v10 Ocelot Provider 不再内置聚合 UI |
| `/admin-api/*` | 同等能力的 REST API（JSON），可被脚本/CI 调用 |

### 备份策略

- 每次写入前在配置文件同目录生成 `configuration.backup.{yyyyMMddHHmmss}.json`
- 自动保留最近 10 份，多余的会被清理
- 写入采用临时文件 + `File.Move(overwrite:true)` 原子替换

### 注意事项

- `System.Text.Json` 不支持保留 JSON 注释，写回后 `configuration.json` 中的 `//` 注释会丢失（备份会保留原文）。
- Basic Auth 仅适合内网/反向代理场景；生产环境建议在反代层加 IP 白名单或换 JWT，并改用强密码。
- 路由编辑使用「下标」定位，避免多终端并发编辑同一份文件。

---

## 📄 许可证

MIT License - 详见 [LICENSE](../../LICENSE)

