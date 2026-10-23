# Event Bus and Integration Events

SyZero provides a unified `IEventBus` abstraction. You can start with in-process events and move to Redis, RabbitMQ, or database-backed delivery when the system grows.

## Backend choices

| Backend | Registration API | Characteristics |
| --- | --- | --- |
| Local | `AddLocalEventBus()` | In-process events, no external dependency |
| DB | `AddDBEventBus()` | Simple persistence and retry support |
| Redis | `AddSyZeroRedis(builder.Configuration)` + `AddRedisEventBus(builder.Configuration)` | Cross-instance broadcast without durable delivery guarantees |
| RabbitMQ | `AddRabbitMQEventBus()` | Standard queue-based async integration for production |

## 1. Define an event

Keep events in a contract-oriented assembly when possible.

```csharp
using SyZero.EventBus;

public class OrderCreatedEvent : EventBase
{
    public long OrderId { get; set; }
    public long UserId { get; set; }
}
```

## 2. Define a handler

```csharp
using SyZero.EventBus;

public class OrderCreatedHandler : IEventHandler<OrderCreatedEvent>
{
    public Task HandleAsync(OrderCreatedEvent @event)
    {
        Console.WriteLine($"order created: {@event.OrderId}");
        return Task.CompletedTask;
    }
}
```

## 3. Pick an event bus implementation

### Local in-process events

```csharp
builder.AddSyZero();
builder.Services.AddLocalEventBus(builder.Configuration);
```

Good for monoliths, domain events, and local verification.

### Database-backed event bus

```csharp
builder.AddSyZero();
builder.Services.AddDBEventBus(builder.Configuration);
```

Good when you need basic persistence and retries without adding a broker yet.

### Redis event bus

```csharp
builder.AddSyZero();
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddRedisEventBus(builder.Configuration, options =>
{
    options.ChannelPrefix = "SyZero:EventBus:";
});
```

Good for lightweight broadcast scenarios such as cross-instance notifications and cache invalidation.

### RabbitMQ event bus

```csharp
builder.AddSyZero();
builder.Services.AddRabbitMQEventBus(builder.Configuration);
```

Configuration example:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/"
  }
}
```

Good for clearer message boundaries in order, payment, notification, and other integration-heavy workflows.

## 4. Publish and subscribe

The most direct pattern is to inject `IEventBus` into application services and subscribe or publish where appropriate.

```csharp
public class OrderAppService : SyZeroServiceBase, IScopedDependency
{
    private readonly IEventBus _eventBus;

    public OrderAppService(IEventBus eventBus)
    {
        _eventBus = eventBus;
        _eventBus.Subscribe<OrderCreatedEvent, OrderCreatedHandler>(() => new OrderCreatedHandler());
    }

    public async Task CreateAsync(long orderId, long userId)
    {
        await _eventBus.PublishAsync(new OrderCreatedEvent
        {
            OrderId = orderId,
            UserId = userId
        });
    }
}
```

If handlers depend on other services, prefer resolving them through the container rather than constructing them manually.

## 5. When to add Outbox

If writing the database and publishing an event must behave more reliably together, add Outbox support:

```csharp
builder.Services.AddSyZeroOutbox();
```

With EF Core in production, switch to the persistent implementation:

```csharp
builder.Services.AddSyZeroOutboxEfCore(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
```

This is the right direction when:

1. Order creation or similar workflows must publish integration events reliably
2. You want less hand-written compensation logic
3. Local transactions and message dispatch need tighter coordination

## 6. Recommended defaults

1. Use `AddLocalEventBus()` for monolith and domain-event scenarios
2. Use `AddRedisEventBus()` for lightweight distributed broadcast
3. Use `AddRabbitMQEventBus()` for reliable production-grade async integration
4. Add `AddSyZeroOutbox()` when transaction consistency matters

## Next

1. See [SyZero.EventBus.RabbitMQ](/en/modules/SyZero.EventBus.RabbitMQ) for broker-specific options
2. See [SyZero.Redis](/en/modules/SyZero.Redis) for Redis event bus and cache features
3. See [SyZero.DistributedTransaction](/en/modules/SyZero.DistributedTransaction) for Outbox details
