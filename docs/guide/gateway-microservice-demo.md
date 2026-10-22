# SyZero.Gateway 微服务网关示例

仓库里的 `src/SyZero.Gateway/` 是一个基于 `SyZero.Gateway.Ocelot` 和 `SyZero.Gateway.Admin` 的网关宿主示例。它适合接在 `Example1`、`Example2` 这类服务前面，统一入口、聚合 Swagger，并通过 `/admin` 在线维护路由。

## 先说明一个现状

仓库当前自带的 [configuration.json](/d:/Project/syzero/SyZero/src/SyZero.Gateway/configuration.json) 默认指向的是 `SyZero.AI`、`SyZero.Authorization` 等服务名，并不是 `Example1` / `Example2`

所以如果你要用它来联调示例微服务，需要先替换成自己的路由配置

## 方案一：最直接的本地联调

这个方案不依赖注册中心，直接把网关下游写死到 `localhost:5002` 和 `localhost:5003`

### 1. 先启动两个下游服务

参考上一页先启动：

1. `SyZero.Example1.Web` -> `http://localhost:5002`
2. `SyZero.Example2.Web` -> `http://localhost:5003`

## 2. 把 `configuration.json` 改成示例路由

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
            "Path": "/swagger/v1/swagger.json"
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
            "Path": "/swagger/v1/swagger.json"
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

## 3. 给 `/admin` 配一个密码

默认 [src/SyZero.Gateway/appsettings.json](/d:/Project/syzero/SyZero/src/SyZero.Gateway/appsettings.json) 里：

```json
"Gateway": {
  "Admin": {
    "Username": "admin",
    "Password": ""
  }
}
```

`Password` 为空时，`/admin` 和 `/admin-api` 会返回 `503`。联调前至少要把它改成一个本地密码，例如：

```json
"Gateway": {
  "Admin": {
    "Username": "admin",
    "Password": "admin123456"
  }
}
```

## 4. 启动网关

```bash
dotnet run --project src/SyZero.Gateway/SyZero.Gateway.csproj
```

仓库里 `launchSettings.json` 使用的是 `http://localhost:5000`，因此可以直接从这里验证

## 5. 验证转发

### 通过网关访问 Example1

1. `http://localhost:5000/example1/api/SyZero.Example1/Example1/GetServiceInfo`
2. `http://localhost:5000/example1/api/SyZero.Example1/Example1/CallExample2Service`

### 通过网关访问 Example2

1. `http://localhost:5000/example2/api/SyZero.Example2/Example2/GetServiceInfo`
2. `http://localhost:5000/example2/api/SyZero.Example2/Example2/CallExample1Service`

## 6. 验证管理页

启动网关后可以访问：

1. `http://localhost:5000/admin`
2. `http://localhost:5000/admin/routes`
3. `http://localhost:5000/admin/swagger-endpoints`

这个管理页适合做下面几件事：

1. 在线增删改查 `Routes`
2. 在线维护 `SwaggerEndPoints`
3. 查看运行时配置与磁盘配置是否漂移
4. 在本地联调时快速试错路由，而不必每次手改 JSON

## 方案二：切到服务发现版网关

如果你不想把下游地址写死，可以把网关改为通过 SyZero 的统一服务发现抽象来找实例。典型思路是：

1. 给网关注册 `AddLocalServiceManagement()`、`AddConsul()`、`AddNacos()` 或 `AddRedisServiceManagement()`
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

### 下游服务正常，但 Swagger 聚合异常

优先检查：

1. `SwaggerEndPoints[*].Config[*].Service.Path` 是否为 `/swagger/v1/swagger.json`
2. 下游服务是否能直接打开 `/api/swagger`
3. 当前是否需要在修改 Swagger 端点后重启网关

## 下一步

1. 继续阅读 [Example1 + Example2 微服务互调示例](/guide/example1-example2-microservices)
2. 继续阅读 [服务注册与发现](/guide/service-discovery)
3. 继续阅读 [SyZero.Gateway.Admin](/modules/SyZero.Gateway.Admin)
