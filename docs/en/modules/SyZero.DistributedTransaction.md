# SyZero.DistributedTransaction

Distributed-transaction toolbox: **Outbox pattern** (eventual consistency between business transaction + event publishing) and a **simple Saga coordinator** (multi-step workflow with reverse compensation).

## 📦 Install

```bash
dotnet add package SyZero.DistributedTransaction
```

## ✨ Features

- 📤 **Outbox pattern** — write events into the outbox table inside the business transaction; a `BackgroundService` polls and publishes them through `IEventBus`.
- 🔁 **Pluggable store** — default `MemoryOutboxStore` (tests only); production must use EF Core / SqlSugar / MongoDB sharing the **same database** as business data.
- ♻️ **Per-message retry counter** — entries hitting `MaxRetryCount` are kept with the last error for manual handling.
- 🧬 **Simple Saga** — `SagaCoordinator<TContext>` runs steps sequentially and calls `CompensateAsync` in reverse order on failure.
- 🔌 **Zero coupling** — depends only on `SyZero.Core`; works with any `IEventBus` backend (Local / DB / Redis / RabbitMQ).

## 🚀 Outbox Quick Start

```csharp
builder.Services.AddSyZeroOutbox(builder.Configuration);

// In your service:
await _outbox.EnqueueAsync(new OutboxMessage
{
    EventName = "order.created",
    PayloadJson = JsonSerializer.Serialize(order)
});
await _db.SaveChangesAsync(); // same transaction as the business write
```

## 🧬 Saga Quick Start

```csharp
var saga = new SagaCoordinator<OrderContext>(logger)
    .Add(new ReserveStockStep())
    .Add(new ChargeStep());

var result = await saga.RunAsync(new OrderContext("O-1", 99m));
```

If `ChargeStep` throws, `ReserveStockStep.CompensateAsync` is invoked.

## ⚙️ Outbox Options

```json
{
  "SyZeroOutbox": {
    "PollIntervalSeconds": 2,
    "BatchSize": 100,
    "MaxRetryCount": 5
  }
}
```

## ⚠️ Notes

- **Replace `IOutboxStore` in production** — the in-memory default does not persist across restarts.
- Outbox table and business tables must live in the **same database** to keep transactional consistency.
- `SagaCoordinator` does not persist intermediate state; it suits short-running workflows. For long-running sagas (manual approval, day-scale waits), persist the state yourself and add custom recovery.
