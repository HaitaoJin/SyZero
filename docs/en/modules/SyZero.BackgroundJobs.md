# SyZero.BackgroundJobs

SyZero background jobs module: `IBackgroundJobManager` abstraction + in-memory default implementation backed by `System.Threading.Channels` + `IHostedService` worker. Zero external dependencies. For persistence / cron scheduling, swap in a child module (`SyZero.BackgroundJobs.Hangfire` / `SyZero.BackgroundJobs.Quartz`, planned).

## Design

| Capability | Implementation |
| --- | --- |
| Job contract | `IBackgroundJob<TArgs>.ExecuteAsync(args, ct)` |
| Enqueue API | `IBackgroundJobManager.EnqueueAsync<TJob, TArgs>(args)` (Singleton) |
| Queue | `BackgroundJobChannel` (bounded `Channel<BackgroundJobItem>`, default capacity 1024, optional `DropOldest`) |
| Worker | `BackgroundJobWorker : BackgroundService`, configurable `WorkerCount`, exceptions other than `OperationCanceledException` are logged via `ILogger.LogError` and swallowed |
| Job lifetime | A new `IServiceScope` is created per execution; `TJob` is resolved as Scoped |

## Quick start

```csharp
builder.Services
    .AddSyZero()
    .AddSyZeroBackgroundJobs(builder.Configuration, opt =>
    {
        opt.QueueCapacity = 4096;
        opt.WorkerCount   = 4;
    });

builder.Services.AddBackgroundJob<SendEmailJob>();
```

`appsettings.json`:

```json
{
  "BackgroundJobs": {
    "QueueCapacity": 4096,
    "WorkerCount": 4,
    "DropOldestWhenFull": false
  }
}
```

## Defining a job

```csharp
public class SendEmailArgs
{
    public string To   { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class SendEmailJob : IBackgroundJob<SendEmailArgs>
{
    private readonly ISmtpSender _smtp;
    public SendEmailJob(ISmtpSender smtp) => _smtp = smtp;

    public Task ExecuteAsync(SendEmailArgs args, CancellationToken ct = default)
        => _smtp.SendAsync(args.To, args.Body, ct);
}
```

## Enqueue

```csharp
await _bg.EnqueueAsync<SendEmailJob, SendEmailArgs>(new SendEmailArgs
{
    To   = input.Email,
    Body = "Your order has been placed"
});
```

## Capacity & backpressure

| Configuration | Behavior |
| --- | --- |
| `QueueCapacity = 0` | Unbounded (OOM risk; local dev only) |
| `QueueCapacity = N`, `DropOldestWhenFull = false` | `EnqueueAsync` waits when full |
| `QueueCapacity = N`, `DropOldestWhenFull = true` | Oldest item dropped when full (no waiting; **may lose jobs**) |

## Failure handling

- Exceptions inside a job are caught and `ILogger.LogError`-ed; **they do not affect other jobs**.
- This module **does not** provide retry / persistence. Either implement retry-with-backoff inside your job, or wait for the planned `SyZero.BackgroundJobs.Hangfire` child module.

## Compatibility

- Target framework: `net10.0`
- Dependencies: `SyZero.Core`, `Microsoft.Extensions.Hosting.Abstractions`
- On host shutdown `BackgroundService.StopAsync` cancels in-flight jobs — please respect the `CancellationToken` inside your jobs.

## Known limitations

- No persistence: in-flight queue is lost on process crash.
- No delayed / cron scheduling.
- No cross-process load balancing: every replica has its own in-memory queue.
