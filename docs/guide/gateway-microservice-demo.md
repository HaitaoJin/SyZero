# SyZero.Gateway 微服务网关示例

仓库里的 `src/SyZero.Gateway/` 是一个基于 `SyZero.Gateway.Ocelot` 和 `SyZero.Gateway.Admin` 的网关宿主示例。它适合接在 `Example1`、`Example2` 这类服务前面，统一入口、聚合 Swagger，并通过 `/admin` 在线维护路由。

仓库内置的 `configuration.json` 已配置 Example1 / Example2 服务发现路由，分别使用 `/example1/*` 与 `/example2/*` 作为统一入口。若希望从浏览器完整体验这条链路，可直接运行 [Example5.Frontend](/guide/example5-gateway-frontend)。

## 推荐：通过 AppHost 一次启动

```bash
dotnet run --launch-profile https --project src/SyZero.AppHost/SyZero.AppHost.csproj
```

该命令会同时启动 Gateway、Example1、Example2、Example3 和 Example5.Frontend。固定入口如下：

| 入口 | 地址 |
| --- | --- |
| Aspire Dashboard | `https://localhost:57289` |
| Gateway | `http://localhost:5000` |
| Example5.Frontend | `http://localhost:5175` |

完整的资源、端口和遥测验证步骤见 [AppHost 本地编排与可观测性](/guide/apphost-observability)。

## 方案一：最直接的本地联调

这个方案不依赖注册中心，直接把网关下游写死到 `localhost:5002` 和 `localhost:5003`

### 1. 先启动两个下游服务

参考上一页先启动：

1. `SyZero.Example1.Web` -> `http://localhost:5002`
2. `SyZero.Example2.Web` -> `http://localhost:5003`

### 2. 把 `configuration.json` 改成示例路由

可以先替换成下面这份最小配置：

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/example1/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete", "Patch" ],
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5002 }
      ],
      "SwaggerKey": "example1"
    },
    {
      "UpstreamPathTemplate": "/example2/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete", "Patch" ],
      "DownstreamPathTemplate": "/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        { "Host": "localhost", "Port": 5003 }
      ],
      "SwaggerKey": "example2"
    }
  ],
  "SwaggerEndPoints": [
    {
      "Key": "example1",
      "Config": [
        {
          "Name": "SyZero.Example1 API",
          "Version": "v1",
          "Service": {
            "Name": "localhost:5002",
            "Path": "/openapi/v1.json"
          }
        }
      ]
    },
    {
      "Key": "example2",
      "Config": [
        {
          "Name": "SyZero.Example2 API",
          "Version": "v1",
          "Service": {
            "Name": "localhost:5003",
            "Path": "/openapi/v1.json"
          }
        }
      ]
    }
  ]
}
```

这份配置的含义是：

1. `/example1/*` 全部转发到 `http://localhost:5002/*`
2. `/example2/*` 全部转发到 `http://localhost:5003/*`
3. Swagger 聚合页里分别挂上两个下游服务文档

### 3. 给 `/admin` 配一个密码

仓库示例的 `src/SyZero.Gateway/appsettings.json` 包含本地开发账号：

```json
"Gateway": {
  "Admin": {
    "Username": "admin",
    "Password": "admin"
  }
}
```

`Password` 为空时，`/admin` 和 `/admin-api` 会返回 `503`。示例密码仅供本地联调，部署前必须通过配置源或环境变量替换为强密码，不要提交真实凭据。

### 4. 启动网关

```bash
dotnet run --project src/SyZero.Gateway/SyZero.Gateway.csproj
```

仓库里 `launchSettings.json` 使用的是 `http://localhost:5000`，因此可以直接从这里验证

### 5. 验证转发

#### 通过网关访问 Example1

1. `http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo`
2. `http://localhost:5000/example1/api/SyZero.Example1/Example1/CallExample2Service`

#### 通过网关访问 Example2

1. `http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo`
2. `http://localhost:5000/example2/api/SyZero.Example2/Example2/CallExample1Service`

### 6. 验证管理页

启动网关后可以访问：

1. `http://localhost:5000/admin`
2. `http://localhost:5000/admin/routes`
3. `http://localhost:5000/admin/swagger-endpoints`
4. `http://localhost:5000/admin/service-interfaces`
5. `http://localhost:5000/admin/tester`

这个管理页适合做下面几件事：

1. 在线增删改查 `Routes`
2. 在线维护 `SwaggerEndPoints`
3. 查看运行时配置与磁盘配置是否漂移
4. 在本地联调时快速试错路由，而不必每次手改 JSON
5. 在“服务接口”页查看注册实例上报的 HTTP 接口，并点击 Path 将方法和路径带入路由测试器

“服务接口”依赖网关与下游服务能够通过同一个 `IServiceManagement` 后端互相发现。在当前示例中，服务上报的是下游路径，例如 `/api/SyZero.Example1/Example1/GetServiceInfo`；由于网关对 Example1 增加了 `/example1` 上游前缀，跳转到测试器后应将 URL 调整为 `/example1/api/SyZero.Example1/Example1/GetServiceInfo` 再发送。

## 方案二：切到服务发现版网关

如果你不想把下游地址写死，可以把网关改为通过 SyZero 的统一服务发现抽象来找实例。典型思路是：

1. 给网关注册 `AddLocalServiceManagement(builder.Configuration)`、`AddConsul(builder.Configuration)`、`AddNacos(builder.Configuration)` 或 `AddRedisServiceManagement(builder.Configuration)`
2. 在 `AddSyZeroOcelot(...)` 中启用 `EnableSyZeroServiceDiscovery = true`
3. 在 `configuration.json` 里把路由切到 `UseServiceDiscovery = true` + `ServiceName = "SyZero.Example1"` 这种写法

这条路线更贴近真实微服务部署；具体配置方式见 [SyZero.Gateway.Ocelot](/modules/SyZero.Gateway.Ocelot)

## 常见问题

### 网关能启动，但 `/admin` 返回 503

检查 `Gateway:Admin:Password` 是否为空

### 网关能启动，但转发 404

优先检查：

1. `UpstreamPathTemplate` 是否与你访问的前缀一致
2. `DownstreamPathTemplate` 是否保留了完整下游路径
3. 下游服务本身是否已经在 `5002` / `5003` 正常启动
4. 是否误用了 `/api/SyZero.Example3/...`；Example3 是单体 SPA，不配置到当前网关

例如，不要访问：

```text
http://localhost:5000/api/SyZero.Example3/Example3/GetServiceInfo
```

正确的网关请求必须带 `/example1` 或 `/example2` 前缀。

### 下游服务正常，但 Swagger 聚合异常

优先检查：

1. `SwaggerEndPoints[*].Config[*].Service.Path` 是否为 `/openapi/v1.json`
2. 下游服务是否能直接打开 `/openapi/v1.json`
3. 当前是否需要在修改 Swagger 端点后重启网关

### “服务接口”页没有数据

优先检查：

1. 网关和下游服务是否注册了同一种 `IServiceManagement` 实现，并连接到同一个服务注册后端
2. 下游服务是否已执行 `app.UseSyZero()` 完成服务注册
3. 下游服务是否使用支持接口元数据上报的版本；升级后需要重启并重新注册

## 下一步

1. 继续阅读 [Example1 + Example2 微服务互调示例](/guide/example1-example2-microservices)
2. 运行 [Example5.Frontend 网关前端示例](/guide/example5-gateway-frontend)
3. 查看 [AppHost 本地编排与可观测性](/guide/apphost-observability)
4. 继续阅读 [服务注册与发现](/guide/service-discovery)
5. 继续阅读 [SyZero.Gateway.Admin](/modules/SyZero.Gateway.Admin)
