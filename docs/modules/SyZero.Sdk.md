# SyZero.Sdk

为业务服务编写「客户端 SDK」提供轻量基类与 DI 扩展。基于 `IHttpClientFactory` typed client，避免每个服务自己造一遍 HTTP 包装。

## 📦 安装

```bash
dotnet add package SyZero.Sdk
```

## 🚀 快速开始

```csharp
public sealed class OrderClient : SyServiceClient
{
    public OrderClient(HttpClient http) : base(http) { }

    public Task<OrderDto?> GetAsync(long id) => GetAsync<OrderDto>($"orders/{id}");

    public Task<OrderDto?> CreateAsync(CreateOrderDto dto) =>
        PostAsync<CreateOrderDto, OrderDto>("orders", dto);
}

// 注册：
builder.Services.AddSyServiceClient<OrderClient>("https://order-service/");
```

## 🧩 与 Resilience 组合

```csharp
builder.Services.AddSyServiceClient<OrderClient>("https://order-service/")
    .AddStandardResilienceHandler(); // 来自 Microsoft.Extensions.Http.Resilience
```
