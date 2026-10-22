# SyZero.BackgroundJobs

SyZero 后台任务模块。`IBackgroundJobManager` 抽象 + 基于 `System.Threading.Channels` 的内存默认实现 + `IHostedService` Worker。开箱即用、零外部依赖。生产环境如需持久化 / Cron 调度，可通过子模块（`SyZero.BackgroundJobs.Hangfire` / `SyZero.BackgroundJobs.Quartz`，规划中）替换。

## 设计概览

| 能力 | 实现 |
|---|---|
| 任务契约 | `IBackgroundJob<TArgs>.ExecuteAsync(args, ct)` |
| 投递入口 | `IBackgroundJobManager.EnqueueAsync<TJob, TArgs>(args)`（Singleton） |
| 队列 | `BackgroundJobChannel`（基于 `Channel<BackgroundJobItem>`），`BoundedChannel` 默认容量 1024，可配 `DropOldest` 策略 |
| Worker | `BackgroundJobWorker : BackgroundService`，可配 `WorkerCount` 并发度，`OperationCanceledException` 之外的异常一律 `ILogger.LogError` 后继续消费 |
| Job 生命周期 | 每次执行新建 `IServiceScope`，从中解析 `TJob`（默认 Scoped，业务也可改 Transient） |

## 快速接入

```csharp
// Program.cs
builder.Services
    .AddSyZero()
    .AddSyZeroBackgroundJobs(builder.Configuration, opt =>
    {
        opt.QueueCapacity = 4096;
        opt.WorkerCount   = 4;
    });

// 业务侧每种 Job 注册一次（Scoped）
builder.Services.AddBackgroundJob<SendEmailJob>();
```

`appsettings.json`：

```json
{
  "BackgroundJobs": {
    "QueueCapacity": 4096,
    "WorkerCount": 4,
    "DropOldestWhenFull": false
  }
}
```

## 定义任务

```csharp
public class SendEmailArgs
{
    public string To   { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class SendEmailJob : IBackgroundJob<SendEmailArgs>
{
    private readonly ISmtpSender _smtp;
    private readonly ILogger<SendEmailJob> _logger;

    public SendEmailJob(ISmtpSender smtp, ILogger<SendEmailJob> logger)
    {
        _smtp = smtp;
        _logger = logger;
    }

    public async Task ExecuteAsync(SendEmailArgs args, CancellationToken ct = default)
    {
        await _smtp.SendAsync(args.To, args.Body, ct);
        _logger.LogInformation("邮件已发送给 {To}", args.To);
    }
}
```

## 投递任务

```csharp
public class OrderAppService : SyZeroServiceBase
{
    private readonly IBackgroundJobManager _bg;
    public OrderAppService(IBackgroundJobManager bg) => _bg = bg;

    public async Task PlaceOrderAsync(CreateOrderDto input)
    {
        // 同步保存订单
        // ...

        // 异步发送通知邮件（不阻塞下单流程）
        await _bg.EnqueueAsync<SendEmailJob, SendEmailArgs>(new SendEmailArgs
        {
            To   = input.Email,
            Body = $"您的订单已提交"
        });
    }
}
```

## 容量与背压

| 配置 | 行为 |
| --- | --- |
| `QueueCapacity = 0` | 无界队列（OOM 风险，仅本地开发时使用） |
| `QueueCapacity = N`, `DropOldestWhenFull = false` | 队列满时 `EnqueueAsync` 异步等待 |
| `QueueCapacity = N`, `DropOldestWhenFull = true` | 队列满时丢弃最老任务（无阻塞，**会丢任务**） |

## 异常与重试

- 任务内部抛异常会被 Worker 捕获并 `ILogger.LogError`（保留 stack），**不会影响其他任务**。
- 本模块**不提供自动重试 / 持久化**。需要这些能力请：
  - 在 Job 实现里做 retry-with-backoff；或
  - 引入 `SyZero.BackgroundJobs.Hangfire` 子模块（规划中）。

## 兼容性

- 目标框架：`net9.0`
- 依赖：`SyZero.Core`、`Microsoft.Extensions.Hosting.Abstractions`
- 关闭进程时 `BackgroundService.StopAsync` 会 cancel 所有正在执行的任务（请在 Job 内尊重 `CancellationToken`）。

## 已知限制

- 无持久化：进程崩溃时未消费的任务会丢失。
- 无延迟 / Cron 调度：固定时间执行请暂以 `IHostedService` + `PeriodicTimer` 实现，或等持久化子模块。
- 不支持跨进程负载均衡：多实例部署时每个实例都有自己的内存队列。
