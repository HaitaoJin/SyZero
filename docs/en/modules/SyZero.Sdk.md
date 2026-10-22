# SyZero.Sdk

Lightweight base class + DI extension to author client SDKs for your SyZero services on top of `IHttpClientFactory` typed clients.

## 📦 Install

```bash
dotnet add package SyZero.Sdk
```

## 🚀 Quick Start

```csharp
public sealed class OrderClient(HttpClient http) : SyServiceClient(http)
{
    public Task<OrderDto?> GetAsync(long id) => GetAsync<OrderDto>($"orders/{id}");
    public Task<OrderDto?> CreateAsync(CreateOrderDto dto) =>
        PostAsync<CreateOrderDto, OrderDto>("orders", dto);
}

builder.Services.AddSyServiceClient<OrderClient>("https://order-service/");
```

Combine with `Microsoft.Extensions.Http.Resilience` for retries:

```csharp
builder.Services.AddSyServiceClient<OrderClient>("https://order-service/")
    .AddStandardResilienceHandler();
```
