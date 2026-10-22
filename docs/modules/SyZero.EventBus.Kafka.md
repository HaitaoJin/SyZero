# SyZero.EventBus.Kafka

基于 [Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet) 的 Kafka 生产者 / 消费者骨架，内置「主 topic + 重试 topic + 死信 topic」三段式失败处理约定。

## 📦 安装

```bash
dotnet add package SyZero.EventBus.Kafka
```

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroKafka(o =>
{
    o.BootstrapServers = "kafka:9092";
    o.ClientId = "order-service";
    o.GroupId = "order-service";
});

// 派生消费者
public sealed class OrderCreatedConsumer : SyKafkaConsumerBase
{
    protected override string Topic => "orders.created";
    public OrderCreatedConsumer(SyKafkaOptions opt, ISyKafkaProducer p, ILogger<OrderCreatedConsumer> log)
        : base(opt, p, log) { }
    protected override Task HandleAsync(string key, string payload, CancellationToken ct)
    {
        // 失败抛异常 → 自动转发到 orders.created.retry → 累计 MaxRetry 次后进 orders.created.dlq
        return Task.CompletedTask;
    }
}

builder.Services.AddHostedService<OrderCreatedConsumer>();
```

## 🔁 重试 / 死信 topic 命名

- 重试主题：`<Topic><RetryTopicSuffix>`，默认后缀 `.retry`
- 死信主题：`<Topic><DeadLetterTopicSuffix>`，默认后缀 `.dlq`

## ⏳ Scheduled retry（退避调度）

失败消息**不会立即重投**。`SyKafkaConsumerBase` 在转发到 `.retry` topic 时会写入两个 header：

- `sy-retry`：当前重试次数（`int`）
- `sy-not-before`：最早可被处理的 Unix 毫秒时间戳

消费者每次从 `.retry` 读到消息时，先读取 `sy-not-before`，未到时间则 `Task.Delay` 到时再调用 `HandleAsync`。

退避算法封装在 `SyKafkaBackoff.Compute(retryCount, baseDelay, maxDelay)`：

- 指数退避：`baseDelay × 2^(retryCount-1)`
- ±20% 随机抖动避免集群同步风暴
- `maxDelay` 上限封顶

可在派生类按消费者粒度覆盖：

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

## 🛠 自定义 header 发布

`ISyKafkaProducer` 同时提供带 `headers` 的重载，便于业务在跨服务消息上写入 trace / 租户 / 版本等元数据：

```csharp
await producer.PublishAsync("orders.created", orderId, json,
    new Dictionary<string, byte[]>
    {
        ["x-tenant-id"] = Encoding.UTF8.GetBytes(tenantId),
        ["x-traceparent"] = Encoding.UTF8.GetBytes(traceParent)
    });
```

## ⚠️ 注意

- 生产者启用幂等（`EnableIdempotence=true`、`Acks=All`）。
- 消费者目前只订阅 `Topic` 与 `Topic + RetryTopicSuffix`；DLQ 不会被自动重新消费，需要业务侧人工处理或单独消费者拉取。
