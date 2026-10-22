# SyZero.Resilience

Polly v8 wrapper providing a SyZero-style timeout / retry / circuit-breaker pipeline.

## 📦 Install

```bash
dotnet add package SyZero.Resilience
```

## 🚀 Quick Start

```csharp
var pipeline = SyResiliencePipelines.Default(
    timeout: TimeSpan.FromSeconds(10),
    retryCount: 3,
    baseDelay: TimeSpan.FromMilliseconds(200));

var data = await pipeline.ExecuteAsync(async ct => await client.GetAsync(ct));
```

Or via DI:

```csharp
builder.Services.AddSyZeroResilience();             // registers "default" pipeline
builder.Services.AddResiliencePipeline("orders", b => b.AddRetry(new() { MaxRetryAttempts = 5 }));

public class OrderService(ResiliencePipelineProvider<string> provider)
{
    public Task PlaceAsync() => provider.GetPipeline("orders").ExecuteAsync(async ct => { });
}
```

`SyResiliencePipelines.Default(...)` = timeout (outer) → exponential-backoff retry with jitter → circuit breaker (50% failure ratio).
