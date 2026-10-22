# SyZero.EventBus.Kafka

Minimal Kafka producer/consumer scaffolding built on [Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet), with a built-in **main + retry + dead-letter** topic convention.

## 📦 Install

```bash
dotnet add package SyZero.EventBus.Kafka
```

## 🚀 Quick start

```csharp
builder.Services.AddSyZeroKafka(o =>
{
    o.BootstrapServers = "kafka:9092";
    o.GroupId = "order-service";
});

public sealed class OrderCreatedConsumer : SyKafkaConsumerBase
{
    protected override string Topic => "orders.created";
    public OrderCreatedConsumer(SyKafkaOptions opt, ISyKafkaProducer p, ILogger<OrderCreatedConsumer> log)
        : base(opt, p, log) { }
    protected override Task HandleAsync(string key, string payload, CancellationToken ct)
        => Task.CompletedTask;   // throw to trigger retry / DLQ
}

builder.Services.AddHostedService<OrderCreatedConsumer>();
```

## 🔁 Topic naming

- Retry: `<Topic><RetryTopicSuffix>` (default `.retry`)
- Dead-letter: `<Topic><DeadLetterTopicSuffix>` (default `.dlq`)

## ⏳ Scheduled retry (back-off)

Failed messages are **not retried immediately**. When `SyKafkaConsumerBase` forwards to `.retry`, it writes two headers:

- `sy-retry` — current retry count (`int`)
- `sy-not-before` — earliest processing time (Unix milliseconds)

When the consumer reads a message off `.retry`, it inspects `sy-not-before` and `Task.Delay`s until that time before invoking `HandleAsync`.

Back-off math is encapsulated in `SyKafkaBackoff.Compute(retryCount, baseDelay, maxDelay)`:

- exponential: `baseDelay × 2^(retryCount-1)`
- ±20% random jitter to avoid synchronized stampedes
- capped at `maxDelay`

Override per consumer:

```csharp
public sealed class OrderCreatedConsumer : SyKafkaConsumerBase
{
    protected override string Topic => "orders.created";
    protected override int MaxRetry => 5;
    protected override TimeSpan RetryBaseDelay => TimeSpan.FromSeconds(5);
    protected override TimeSpan RetryMaxDelay => TimeSpan.FromMinutes(10);
    // ...
}
```

## 🛠 Custom headers

`ISyKafkaProducer` also exposes a `headers` overload — handy for trace context, tenant ids, schema versions, etc.:

```csharp
await producer.PublishAsync("orders.created", orderId, json,
    new Dictionary<string, byte[]>
    {
        ["x-tenant-id"] = Encoding.UTF8.GetBytes(tenantId),
        ["x-traceparent"] = Encoding.UTF8.GetBytes(traceParent)
    });
```

The producer is configured with `EnableIdempotence=true` and `Acks=All`.

> The consumer subscribes to `Topic` and `Topic + RetryTopicSuffix` only. DLQ messages are **not** consumed back automatically; handle them via tooling or a separate dedicated consumer.
