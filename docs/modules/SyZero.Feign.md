# SyZero.Feign

SyZero 框架的声明式 HTTP 客户端模块，基于 Refit 实现类似 Spring Cloud OpenFeign 的功能。自 `v9.1.0` 起，`SyZero.Feign` 是 [`IRemoteServiceProvider`](./SyZero.Core) 抽象的一个具体实现（Provider）—— 同一份 `RemoteServiceOptions` 配置可被未来的其它 Provider（如 WebApiClient 等）复用。

## 📦 安装

```bash
dotnet add package SyZero.Feign
```

## ✨ 特性

- 🚀 **声明式调用** - 使用接口定义 HTTP 客户端
- 💾 **服务发现** - 集成服务注册与发现
- 🔒 **负载均衡** - 内置负载均衡支持
- 🔄 **gRPC 支持** - 同时支持 HTTP 和 gRPC 调用
- 🧩 **协议级 Provider 扩展** - 可通过 `IRemoteServiceProxyFactory` 注入自定义协议工厂

---

## 🚀 快速开始

### 1. 配置 appsettings.json

```json
{
  "Feign": {
    "Service": [
      {
        "ServiceName": "SyZero.Example2",
        "AssemblyName": "SyZero.Example2.IApplication",
        "Protocol": "Http",
        "Timeout": 30,
        "Retry": 3
      }
    ],
    "Global": {
      "Protocol": "Http",
      "Timeout": 30,
      "Retry": 3,
      "EnableSsl": false
    }
  }
}
```

> ⚠️ **`v9.1.0` 统一发布时纳入的破坏性变更**：原 `FeignOptions` / `FeignService` / `FeignProtocol` / `IFeignProxyFactory` 等类型被下沉到 `SyZero.Core` 并改名为 `RemoteServiceOptions` / `RemoteServiceItem` / `RemoteServiceProtocol` / `IRemoteServiceProxyFactory`；服务条目字段 `DllName` 改名为 `AssemblyName`。`AddSyZeroFeign(...)` API 形态保持不变（参数类型由 `FeignOptions` 改为 `RemoteServiceOptions`）。

### 2. 注册服务

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
// 添加SyZero
builder.AddSyZero();

// 注册服务方式1 - 从配置 "Feign" 节读取
builder.Services.AddSyZeroFeign(builder.Configuration);

// 注册服务方式2 - 配置 + 委托
builder.Services.AddSyZeroFeign(options =>
{
    options.Global.Timeout = 30;
    options.Global.Retry = 3;
}, builder.Configuration);

// 注册服务方式3 - 直接传入 RemoteServiceOptions
builder.Services.AddSyZeroFeign(new RemoteServiceOptions
{
    Service =
    {
        new RemoteServiceItem
        {
            ServiceName = "user-service",
            AssemblyName = "User.IApplication",
            Protocol = RemoteServiceProtocol.Http
        }
    }
});

// 也可直接走 Core 的 Provider 注册入口（与上面等价）：
// builder.Services.AddRemoteService<FeignRemoteServiceProvider>(builder.Configuration);

var app = builder.Build();
// 使用SyZero
app.UseSyZero();
app.Run();
```

### 3. 使用示例

```csharp
[FeignClient("user-service")]
public interface IUserServiceClient
{
    [Get("/api/user/{id}")]
    Task<UserDto> GetUserAsync(long id);

    [Post("/api/user")]
    Task<UserDto> CreateUserAsync([Body] CreateUserInput input);
}

public class MyService
{
    private readonly IUserServiceClient _userClient;

    public MyService(IUserServiceClient userClient)
    {
        _userClient = userClient;
    }

    public async Task<UserDto> GetUserAsync(long id)
    {
        return await _userClient.GetUserAsync(id);
    }
}
```

---

## 📖 配置选项

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Timeout` | `int` | `30000` | 请求超时时间（毫秒） |
| `RetryCount` | `int` | `3` | 重试次数 |
| `BaseUrl` | `string` | `""` | 基础地址（不使用服务发现时） |

---

## 📖 API 说明

### FeignClient 特性

| 属性 | 说明 |
|------|------|
| `Name` | 服务名称（用于服务发现） |
| `Url` | 固定地址（不使用服务发现） |
| `FallbackType` | 降级处理类型 |

> 使用特性标记接口方法的 HTTP 方法和路径

---

## 🔧 高级用法

### 请求拦截器

```csharp
builder.Services.AddSyZeroFeign(builder.Configuration)
    .AddInterceptor<AuthHeaderInterceptor>();

public class AuthHeaderInterceptor : IRequestInterceptor
{
    public Task InterceptAsync(HttpRequestMessage request)
    {
        request.Headers.Add("Authorization", "Bearer xxx");
        return Task.CompletedTask;
    }
}
```

### gRPC 调用

```csharp
[FeignClient("user-service", Protocol = Protocol.Grpc)]
public interface IUserGrpcClient
{
    Task<UserDto> GetUserAsync(long id);
}
```

---

## ⚠️ 注意事项

1. **服务发现** - 使用服务名时需要配置服务发现组件
2. **超时配置** - 根据业务合理配置超时时间
3. **降级处理** - 建议实现 Fallback 以提高可用性

---

## 📄 许可证

MIT License - 详见 [LICENSE](../../LICENSE)
