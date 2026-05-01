# SyZero.Resilience

基于 [Polly v8](https://github.com/App-vNext/Polly) 的韧性策略包装：超时 / 重试 / 熔断的「SyZero 风格」组合。

## 📦 安装

```bash
dotnet add package SyZero.Resilience
```

## 🚀 快速开始

### 1) 直接使用静态构建器

```csharp
using SyZero.Resilience;

var pipeline = SyResiliencePipelines.Default(
    timeout: TimeSpan.FromSeconds(10),
    retryCount: 3,
    baseDelay: TimeSpan.FromMilliseconds(200));

var data = await pipeline.ExecuteAsync(async ct =>
{
    return await client.GetAsync(ct);
});
```

### 2) 通过 DI 注册命名管道

```csharp
builder.Services.AddSyZeroResilience(); // 注册名为 "default" 的管道
builder.Services.AddResiliencePipeline("orders", b =>
{
    b.AddRetry(new() { MaxRetryAttempts = 5, Delay = TimeSpan.FromMilliseconds(100) });
});

public class OrderService(ResiliencePipelineProvider<string> provider)
{
    public async Task PlaceAsync()
    {
        var pipeline = provider.GetPipeline("orders");
        await pipeline.ExecuteAsync(async ct => { /* ... */ });
    }
}
```

## 🧩 默认管道组合

`SyResiliencePipelines.Default(...)` = 超时（外层兜底） → 重试（指数退避 + 抖动） → 熔断（采样窗口内失败率超 50% 触发）。
