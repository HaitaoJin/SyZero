# AppHost 本地编排与可观测性

`src/SyZero.AppHost/` 使用 .NET Aspire 同时启动示例服务、网关和前端，并把结构化日志、分布式跟踪与指标汇总到 Aspire Dashboard。它是验证 SyZero 微服务调用链的推荐入口。

## 运行内容与固定端口

| 资源 | 地址 | 用途 |
| --- | --- | --- |
| Aspire Dashboard | `https://localhost:57289` | 资源、日志、跟踪和指标 |
| Dashboard HTTP | `http://localhost:57288` | 不使用 HTTPS 时的 Dashboard |
| Gateway | `http://localhost:5000` | Example1、Example2 的统一入口 |
| Example1 | `http://localhost:5002` | 微服务示例一 |
| Example2 | `http://localhost:5003` | 微服务示例二 |
| Example3 | `http://localhost:5004` | 单体 SPA 后端宿主 |
| Example3 Vite | `http://localhost:5174` | Example3 开发期 SPA |
| Example5.Frontend | `http://localhost:5175` | 独立网关前端示例 |

Dashboard 的 OTLP 接收端点固定为 `57290`，资源服务端点固定为 `57291`。这些端口定义在 `SyZero.AppHost/Properties/launchSettings.json`；Example5 使用 Vite 的 `strictPort`，端口被占用时会明确失败，不会静默切换到随机端口。

## 启动

需要安装 .NET 10 SDK 和 Node.js。首次运行前不必分别启动各个示例：

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

浏览器会打开 `https://localhost:57289`。资源页应包含：

- `example1`
- `example2`
- `example3`
- `gateway`
- `example5-frontend`

`example5-frontend` 只有一个长期运行资源；依赖安装属于启动准备过程，不会额外保留一个已完成的 `npm install` 资源。

## 验证完整调用链

打开 `http://localhost:5175`，依次执行“刷新服务状态”和“调用全部”。也可以直接请求下面四个网关地址：

```text
GET http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo
GET http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo
GET http://localhost:5000/example1/api/SyZero.Example1/Example1/CallExample2Service
GET http://localhost:5000/example2/api/SyZero.Example2/Example2/CallExample1Service
```

前两个请求验证浏览器到网关再到下游服务；后两个请求还会触发 Example1 与 Example2 之间的 Feign 调用。成功时均返回 HTTP 200。

## 查看日志、跟踪和指标

示例宿主通过下面两部分接入遥测：

```csharp
builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddSyZeroOpenTelemetry(builder.Configuration);
```

由 AppHost 启动时，Aspire 会自动注入标准 OTLP 环境变量，应用不需要把 Dashboard 地址写进 `appsettings.json`。

### 结构化日志

进入 Dashboard 的“结构化日志”，选择 `gateway`、`example1` 或 `example2`。调用接口后应看到请求日志；如果当前日志级别为 `Warning`，普通 `Information` 日志不会显示，可在开发配置中按需调整 `Logging:LogLevel`。

### 分布式跟踪

进入“跟踪”后调用 Example5 的服务互调功能。一条完整链路通常包含：

```text
gateway → example1 → example2
gateway → example2 → example1
```

点击某条跟踪可查看每个入站请求和出站 HTTP 调用的耗时、状态码与 TraceId。

### 指标

进入“指标”，先选择一个后端资源，再选择 ASP.NET Core、Kestrel 或 `System.Net.Http` 指标。指标需要先有请求样本；刚启动且没有流量时列表为空是正常现象。

## 常见问题

### Dashboard 地址每次启动不同

使用仓库提供的 `https` 或 `http` launch profile，不要使用临时生成的宿主参数：

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

固定地址分别是 `https://localhost:57289` 和 `http://localhost:57288`。

### 日志、跟踪或指标为空

按顺序检查：

1. 资源页中的目标服务是否为 `Running`。
2. 是否已经访问 API 产生流量。
3. 应用是否同时注册日志 OpenTelemetry provider 和 `AddSyZeroOpenTelemetry(...)`。
4. 是否通过 AppHost 启动；单独启动时必须自行配置 `OpenTelemetry:OtlpUrl` 或 `OTEL_EXPORTER_OTLP_ENDPOINT`。
5. `AspNetCorePathPrefixes` / `HttpClientPathPrefixes` 是否过滤掉了当前路径。

### 跟踪中出现周期性红色 `gateway: GET`

这通常表示网关健康检查访问了不可达的注册地址。v10 的 `LocalServiceManagement` 会把 `localhost`、回环地址和本机网卡地址归并为同一实例，并统一通过 `localhost` 健康检查，避免 Docker/WSL 虚拟网卡地址造成重复失败。升级后重启 AppHost；重启前已经采集的红色记录是历史数据，不会自动消失。

### 启动时出现两条 `example3: GET` 红色记录

Example3 是前后端同宿主的 SPA 示例。后端第一次探测 `localhost:5174` 时，Vite 可能仍在启动，因而留下少量约 2 秒的失败记录；Vite 就绪后探测会成功。这是一次性的启动过程，不应持续出现。

### 网关请求返回 404

不要把 Example3 路径放到网关中。当前网关只暴露 `/example1/*` 与 `/example2/*`；确认 Gateway 在 `5000` 运行，并使用本页列出的完整路径。

## 延伸阅读

- [Example5.Frontend 网关前端示例](/guide/example5-gateway-frontend)
- [SyZero.Gateway 微服务网关示例](/guide/gateway-microservice-demo)
- [服务注册与发现](/guide/service-discovery)
- [SyZero.Observability.OpenTelemetry](/modules/SyZero.Observability.OpenTelemetry)
