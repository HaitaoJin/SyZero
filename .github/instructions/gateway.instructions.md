---
description: SyZero.Gateway（基于 Ocelot 的 API 网关示例）的配置与扩展约束
applyTo: "src/SyZero.Gateway/**/*.cs,src/SyZero.Gateway/**/*.json,src/SyZero.Gateway/**/*.csproj"
---

# API 网关约束

适用范围：[src/SyZero.Gateway/](../../src/SyZero.Gateway)。这是 **示例性质** 的 Ocelot 网关，演示 `SyZero.Gateway.Ocelot` 模块（`IGateway` 抽象的 Ocelot 实现）的用法。

## 关键事实

- 路由配置在 [configuration.json](../../src/SyZero.Gateway/configuration.json)（**不是** `appsettings.json`），由 `Program.cs` 通过 `AddJsonFile("configuration.json", ...)` 显式加载——遵循 Ocelot 配置约定。
- `appsettings.json` 仅承载 `SyZero` / `Logging` / `GlobalConfiguration` / `OpenTelemetry` 等顶层运行时配置。
- 网关启用顺序固定：`AddSyZero()` → `AddSyZeroOpenTelemetry()` → `AddSyZeroOcelot(...)` → `UseSyZero()` → `UseRouting/UseCors/UseStaticFiles/MapControllers` → `await UseSyZeroOcelotAsync()`。**不要重排**，否则路由/CORS/服务发现会失效。

## 必须遵守

- 端口与协议从 [appsettings.json](../../src/SyZero.Gateway/appsettings.json) 的 `SyZero` 节点读取，并通过 `builder.WebHost.UseUrls(...)` 应用——**不要**硬编码端口。
- CORS 域名以**逗号分隔**写入 `SyZero.CorsOrigins`，由 `AddSyZeroOcelot` 内部解析，使用 `gatewayOptions.CorsPolicyName` 应用。
- 服务发现：通过 `GlobalConfiguration.ServiceDiscoveryProvider` 配置 Consul/Nacos；`Type` 取值需与 `SyZero.Gateway.Ocelot` 支持的提供方一致。
- 修改 [Program.cs](../../src/SyZero.Gateway/Program.cs) 时保留 `await app.UseSyZeroOcelotAsync()` 与 `await app.RunAsync()`（Ocelot 启动是异步的）。
- `PackageReference` **不写版本**（中央版本管理，见 [Directory.Packages.props](../../Directory.Packages.props)）。

## 不要做

- 不要把 Ocelot 路由规则塞进 `appsettings.json`——保持 `configuration.json` 单一职责。
- 不要把网关业务化（添加领域模型、仓储）——网关只做路由 / 鉴权 / 限流 / 聚合 / Swagger 转发。
- 不要在网关里直接引用业务服务项目（`src/SyZero.Service/**`），通信必须走 HTTP / gRPC。
- 不要禁用 OpenTelemetry / Log4Net 集成，除非用户明确要求——它们是网关示例的关键演示点。
