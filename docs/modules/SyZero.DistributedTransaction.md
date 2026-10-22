# SyZero.DistributedTransaction

分布式事务工具集：**Outbox 模式**（保证「业务事务 + 事件发布」最终一致）+ **简易 Saga 编排器**（多服务协同 + 反向补偿）。

## 📦 安装

```bash
dotnet add package SyZero.DistributedTransaction
```

## ✨ 特性

- 📤 **Outbox 模式**：业务事务里把事件写 outbox 表 → 后台 BackgroundService 拉取 → 发布到 `IEventBus` → 标记已发送。
- 🔁 **可插拔存储**：默认 `MemoryOutboxStore`（仅测试）；生产请用 EF Core / SqlSugar / MongoDB 实现，**与业务数据同库同事务**。
- ♻️ **失败重试 + 上限**：每条消息独立重试计数，达到 `MaxRetryCount` 后保留错误等待人工介入。
- 🧬 **简易 Saga 编排**：`SagaCoordinator<TContext>` 顺序执行步骤，任一步失败则按已执行步骤的**反向顺序**调用 `CompensateAsync`。
- 🔌 **零业务侵入**：仅依赖 `SyZero.Core`（含 `IEventBus`），可与任意 EventBus 实现（Local / DB / Redis / RabbitMQ）配合。

---

## 🚀 Outbox 快速开始

### 1. 注册 Outbox 投递

```csharp
builder.Services.AddSyZeroOutbox(builder.Configuration);
// 或：builder.Services.AddSyZeroOutbox(configure: o =>
//     {
//         o.PollIntervalSeconds = 1;
//         o.BatchSize = 50;
//         o.MaxRetryCount = 10;
//     });
```

### 2. 业务里写入 outbox（与业务数据同事务）

```csharp
public class OrderService
{
    private readonly MyDbContext _db;
    private readonly IOutboxStore _outbox;

    public async Task PlaceOrderAsync(Order order)
    {
        using var tx = await _db.Database.BeginTransactionAsync();
        _db.Orders.Add(order);
        await _outbox.EnqueueAsync(new OutboxMessage
        {
            EventName = "order.created",
            PayloadJson = JsonSerializer.Serialize(order)
        });
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
```

后台 `OutboxBackgroundService` 会定时（默认每 2 秒）扫表并发布到 `IEventBus`，发布成功后标记 `PublishedAtUtc`。

---

## 🧬 Saga 快速开始

```csharp
public record OrderContext(string OrderId, decimal Amount);

public class ReserveStockStep : ISagaStep<OrderContext>
{
    public string Name => nameof(ReserveStockStep);
    public Task ExecuteAsync(OrderContext ctx, CancellationToken ct = default) => /* 占用库存 */;
    public Task CompensateAsync(OrderContext ctx, CancellationToken ct = default) => /* 释放库存 */;
}

public class ChargeStep : ISagaStep<OrderContext>
{
    public string Name => nameof(ChargeStep);
    public Task ExecuteAsync(OrderContext ctx, CancellationToken ct = default) => /* 扣款 */;
    public Task CompensateAsync(OrderContext ctx, CancellationToken ct = default) => /* 退款 */;
}

var saga = new SagaCoordinator<OrderContext>(logger)
    .Add(new ReserveStockStep())
    .Add(new ChargeStep());

var result = await saga.RunAsync(new OrderContext("O-1", 99m));
if (!result.IsSuccess)
{
    // result.Error / result.CompensationErrors
}
```

任意一步抛出异常时，编排器会按**反向顺序**调用此前已执行步骤的 `CompensateAsync`。

---

## ⚙️ Outbox 配置项

```json
{
  "SyZeroOutbox": {
    "PollIntervalSeconds": 2,
    "BatchSize": 100,
    "MaxRetryCount": 5
  }
}
```

---

## ⚠️ 注意

- **生产必须替换 `IOutboxStore`**：默认 `MemoryOutboxStore` 没有持久化，进程重启数据丢失。
- Outbox 表与业务表必须**同库**（PostgreSQL / SQL Server / MySQL）才能保证事务一致性。
- `SagaCoordinator` 不持久化中间状态，仅适合**短运行型**协调；长运行型 Saga（人工审核 / 日级别等待）需要把状态写库 + 自定义恢复逻辑。
- 投递目标默认是 `IEventBus.Publish(string eventName, object eventData)`，业务侧需注册对应的 `IDynamicEventHandler`。
