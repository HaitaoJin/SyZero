# SyZero.Gateway.Abstractions

> 自 v1.6.0 引入的网关抽象层。在此之前，相同抽象（命名空间 `SyZero.Gateway`）位于 `SyZero.Core` 包内；为了让 `SyZero.Core` 真正聚焦于框架内核（DI / 仓储 / 服务管理 / 事件总线 / 多级缓存），网关相关抽象被独立成本包。

`SyZero.Gateway.Abstractions` 只包含**接口与 DTO**，不引入任何具体网关依赖。所有 SyZero 网关实现（`SyZero.Gateway.Ocelot`、`SyZero.Gateway.YARP`）以及管理面板（`SyZero.Gateway.Admin`）都通过本包共享同一套抽象，从而实现「实现可插拔、面板共用、配置 schema 一致」。

## 安装

```bash
dotnet add package SyZero.Gateway.Abstractions
```

> 通常你不会直接引用本包 —— 应当引用一个具体实现（`SyZero.Gateway.Ocelot` 或 `SyZero.Gateway.YARP`），它们会自动将 `SyZero.Gateway.Abstractions` 作为依赖一并引入。只有当你**自己实现一个新的 `IGateway` 后端**时，才需要直接引用本包。

## 命名空间

整个抽象层位于命名空间 `SyZero.Gateway`（与包名不同，故意保留以兼容老版本 `using SyZero.Gateway;`）。

```csharp
using SyZero.Gateway;
```

## 核心抽象

| 抽象 | 用途 | 典型实现 |
|---|---|---|
| `IGateway` | 当前生效的网关运行时（`ProviderName` + `GetConfigurationAsync`） | `OcelotGateway`、`YarpGateway` |
| `IGatewayConfigurationStore` | 网关配置持久化（读 / 写 / 重载 / 备份生命周期） | `FileGatewayConfigurationStore`（`SyZero.Gateway.Admin`）、`OcelotConfigurationStore`、`YarpConfigurationStore` |
| `IGatewayConfigurationApplier` | 把 `GatewayConfiguration` 热下发到运行中的网关管线 | `OcelotConfigurationApplier`、`YarpConfigurationApplier` |
| `GatewayConfiguration` | 网关顶层配置 DTO（`Routes` + `SwaggerEndPoints` + `Extra`，含全局限流桥） | — |
| `GatewayRoute` | 单条路由（含 `Disabled` / `Tags` / 限流 / 负载均衡） | — |
| `GatewaySwaggerEndPoint` | Swagger 聚合端点 | — |
| `GatewayOptions` | 网关公共选项基类（CORS / Swagger 标题 / Service 段读取） | `SyZeroOcelotOptions`、`SyZeroYarpOptions` 继承 |
| `GatewayLoadBalancerOptions` / `GatewayRateLimitOptions` / `GatewayGlobalRateLimitOptions` | 与 ocelot.json 对齐的 DTO，便于多后端共用同一份 `configuration.json` | — |

## 设计要点

- **schema 与 ocelot.json 兼容**：所有 DTO 字段命名沿用 PascalCase 习惯，`SyZero.Gateway.Ocelot` 与 `SyZero.Gateway.YARP` 共享同一份 `configuration.json`，实现间可平滑切换。
- **`Extra` 透传**：`GatewayConfiguration` 通过 `[JsonExtensionData]` 保留未识别的根级字段（如 `GlobalConfiguration`），保证写回时不丢失。
- **可选备份能力**：`IGatewayConfigurationStore` 暴露 `ListBackupsAsync` / `ReadBackupAsync` / `RestoreBackupAsync` / `DeleteBackupAsync` 默认接口方法（默认抛 `NotSupportedException`，由 `SyZero.Gateway.Admin` 的 `FileGatewayConfigurationStore` 提供完整实现）。

## 自实现一个网关后端

```csharp
public sealed class MyCustomGateway : IGateway
{
    public string ProviderName => "MyCustom";

    public Task<GatewayConfiguration> GetConfigurationAsync(CancellationToken ct = default)
    {
        // 从你的运行时取出当前生效的路由，转换为 GatewayConfiguration 返回
        return Task.FromResult(new GatewayConfiguration());
    }
}
```

实现 `IGatewayConfigurationStore` + `IGatewayConfigurationApplier`，并在你的扩展方法里注册：

```csharp
services.AddSingleton<IGateway, MyCustomGateway>();
services.AddSingleton<IGatewayConfigurationStore, MyCustomConfigStore>();
services.AddSingleton<IGatewayConfigurationApplier, MyCustomApplier>();
```

之后 `SyZero.Gateway.Admin` 即可直接管理你的自定义网关，无需任何额外适配。

## 历史

- v1.6.0：从 `SyZero.Core` 抽出，作为独立 NuGet 包发布。命名空间 `SyZero.Gateway` 保持不变 —— 业务 `using` 无需修改，只需在引用了 `SyZero.Core` 的项目里改为引用具体实现包（或本抽象包）。
