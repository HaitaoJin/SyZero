# Example1 + Example2 微服务互调示例

`SyZero.Example1.Service` 和 `SyZero.Example2.Service` 是仓库里最直接的微服务示例：两边都通过 `DynamicWebApi` 暴露接口，通过 `SyZero.Feign` 做远程调用，并使用 `LocalServiceManagement` 作为本地联调时的服务发现后端。

## 拓扑

```text
SyZero.Example1.Web  http://localhost:5002
  ├─ DynamicWebApi
  ├─ SqlSugar + SQLite
  ├─ LocalServiceManagement
  └─ Feign -> SyZero.Example2

SyZero.Example2.Web  http://localhost:5003
  ├─ DynamicWebApi
  ├─ SqlSugar + SQLite
  ├─ LocalServiceManagement
  └─ Feign -> SyZero.Example1
```

## 代码里已经配置好的内容

这两个示例默认已经具备以下配置：

1. `Example1` 端口是 `5002`，服务名是 `SyZero.Example1`
2. `Example2` 端口是 `5003`，服务名是 `SyZero.Example2`
3. 两边都启用了 `AddDynamicWebApi(...)`
4. 两边都启用了 `AddSyZeroFeign()`
5. 两边都启用了 `AddLocalServiceManagement()`
6. 两边都在 `appsettings.json` 里配置了对方的 `Feign:Service`

因此本地运行时，不需要先接 Consul / Nacos，就能演示双服务互调

## 1. 启动 Example1

```bash
dotnet run --project src/SyZero.Service/SyZero.Example1.Service/SyZero.Example1.Web/SyZero.Example1.Web.csproj
```

启动后可访问：

1. `http://localhost:5002/api/swagger`
2. `http://localhost:5002/api/SyZero.Example1/Example1/GetServiceInfo`

## 2. 启动 Example2

```bash
dotnet run --project src/SyZero.Service/SyZero.Example2.Service/SyZero.Example2.Web/SyZero.Example2.Web.csproj
```

启动后可访问：

1. `http://localhost:5003/api/swagger`
2. `http://localhost:5003/api/SyZero.Example2/Example2/GetServiceInfo`

## 3. 验证双向远程调用

当两个服务都起来之后，可以直接调用下面这些接口：

### 从 Example1 调 Example2

1. `GET http://localhost:5002/api/SyZero.Example1/Example1/CallExample2Service`
2. `GET http://localhost:5002/api/SyZero.Example1/Example1/GetExample2Data?id=1`

### 从 Example2 调 Example1

1. `GET http://localhost:5003/api/SyZero.Example2/Example2/CallExample1Service`
2. `GET http://localhost:5003/api/SyZero.Example2/Example2/GetExample1Data?id=1`

这几组接口分别演示了：

1. 只拿对端服务元信息
2. 通过 Feign 调用对端业务接口
3. 由对端在本地库中按需创建测试数据并返回 DTO

## 4. 关键实现点

### DynamicWebApi

两边都把 `DefaultAreaName` 设置成了自己的服务名，因此路由前缀是：

1. `Example1` -> `/api/SyZero.Example1/...`
2. `Example2` -> `/api/SyZero.Example2/...`

### Feign

`Example1` 的 `appsettings.json` 中已经声明：

```json
"Feign": {
  "Service": [
    {
      "ServiceName": "SyZero.Example2",
      "AssemblyName": "SyZero.Example2.IApplication",
      "Protocol": "Http"
    }
  ]
}
```

`Example2` 则反向声明 `SyZero.Example1`

### 服务发现

本地联调默认使用 `LocalServiceManagement`。这意味着：

1. 不依赖外部注册中心
2. 两个服务启动后会各自把实例信息注册到本地管理后端
3. Feign 调用时通过统一的 `IServiceManagement` 抽象找对端实例

## 5. 何时切到 Consul / Nacos

如果你要把这个示例扩展成更接近生产环境的拓扑，可以按下面的方向切换：

1. 把 `AddLocalServiceManagement()` 换成 `AddConsul()` 或 `AddNacos()`
2. 保持 `AddSyZeroFeign()` 不变
3. 继续复用相同的 `ServiceName`

这样可以验证 SyZero 的“业务代码不变，只替换服务发现后端”的设计

## 6. 常见问题

### 调用对端失败

优先检查：

1. 两个服务是否都已经启动
2. `5002` 和 `5003` 端口是否被占用
3. 当前访问的路由是否带了完整 area 前缀
4. 本地是否改动过 `Feign:Service[*].ServiceName`

### Swagger 能打开，但互调失败

这通常说明：

1. `DynamicWebApi` 正常
2. `Feign` 或服务发现链路未命中

这种情况下优先对比两个服务的 `SyZero:Name` 与 `Feign:Service[*].ServiceName` 是否一致

## 下一步

1. 如果要把两个服务收口到统一入口，继续阅读 [SyZero.Gateway 微服务网关示例](/guide/gateway-microservice-demo)
2. 如果要切到注册中心，继续阅读 [服务注册与发现](/guide/service-discovery)
3. 如果要查看 Feign 配置细节，继续阅读 [SyZero.Feign](/modules/SyZero.Feign)
