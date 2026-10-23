# 事件总线与集成事件

SyZero 提供统一的 `IEventBus` 抽象，可以先在进程内发布事件，再平滑切到 Redis、RabbitMQ 或基于数据库的持久化事件总线。

## 后端选择

| 后端 | 注册方式 | 特点 |
| --- | --- | --- |
| Local | `AddLocalEventBus()` | 进程内事件，零外部依赖 |
| DB | `AddDBEventBus()` | 简单持久化、可重试 |
| Redis | `AddSyZeroRedis(builder.Configuration)` + `AddRedisEventBus(builder.Configuration)` | 跨实例广播，轻量但不保证持久化 |
| RabbitMQ | `AddRabbitMQEventBus()` | 标准消息队列，适合生产异步解耦 |

## 1. 定义事件

建议把事件放在业务契约清晰、易于复用的程序集里。

```csharp
using SyZero.EventBus;

public class OrderCreatedEvent : EventBase
{
    public long OrderId { get; set; }
    public long UserId { get; set; }
}
```

## 2. 定义事件处理器

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

## 3. 选择一个事件总线实现

### 本地进程内事件

```csharp
builder.AddSyZero();
builder.Services.AddLocalEventBus(builder.Configuration);
```

适合单体服务、领域事件、本地开发验证。

### 数据库事件总线

```csharp
builder.AddSyZero();
builder.Services.AddDBEventBus(builder.Configuration);
```

适合需要持久化和基础重试、但暂时不引入 MQ 的环境。

### Redis 事件总线

```csharp
builder.AddSyZero();
builder.Services.AddSyZeroRedis(builder.Configuration);
builder.Services.AddRedisEventBus(builder.Configuration, options =>
{
    options.ChannelPrefix = "SyZero:EventBus:";
});
```

适合多实例广播通知、缓存失效广播这类轻量异步场景。

### RabbitMQ 事件总线

```csharp
builder.AddSyZero();
builder.Services.AddRabbitMQEventBus(builder.Configuration);
```

配置示例：

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

适合订单、支付、通知等需要更明确消息边界的业务。

## 4. 发布与订阅

当前最直接的写法是在业务服务中注入 `IEventBus`，并在合适位置完成订阅与发布。

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

如果你的处理器本身依赖其他服务，建议通过工厂从容器解析，避免手工 `new`。

## 5. 什么时候要配合 Outbox

当“写数据库”和“发事件”必须具备更强一致性时，建议引入 Outbox：

```csharp
builder.Services.AddSyZeroOutbox();
```

生产环境如果使用 EF Core，可以进一步切到持久化实现：

```csharp
builder.Services.AddSyZeroOutboxEfCore(opt =>
{
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
});
```

这条链路适合：

1. 订单创建后必须可靠投递集成事件
2. 不希望业务层手写补偿逻辑
3. 需要把消息发送和本地事务串联起来

## 6. 选择建议

1. 单体或领域内事件优先 `AddLocalEventBus()`
2. 轻量分布式广播优先 `AddRedisEventBus()`
3. 可靠异步解耦优先 `AddRabbitMQEventBus()`
4. 事务一致性要求更高时，额外接入 `AddSyZeroOutbox()`

## 下一步

1. 查看 [SyZero.EventBus.RabbitMQ](/modules/SyZero.EventBus.RabbitMQ) 了解 MQ 侧配置
2. 查看 [SyZero.Redis](/modules/SyZero.Redis) 了解 Redis 事件总线与缓存能力
3. 查看 [SyZero.DistributedTransaction](/modules/SyZero.DistributedTransaction) 了解 Outbox
