# SyZero.BackgroundJobs.Quartz

Quartz-based Cron / scheduling backend for `SyZero.BackgroundJobs`, reusing the unified `IBackgroundJob<TArgs>` abstraction.

## 📦 Install

```bash
dotnet add package SyZero.BackgroundJobs.Quartz
```

## 🚀 Quick Start

```csharp
builder.Services.AddSyZeroBackgroundJobs(builder.Configuration);
builder.Services.AddBackgroundJob<DailyReportJob>();

builder.Services.AddSyZeroBackgroundJobsQuartz(q =>
{
    q.AddSyZeroCron<DailyReportJob, DailyReportArgs>("0 0 2 * * ?", new DailyReportArgs { Date = "today" });
});
```

Args travel through `JobDataMap` as JSON; ensure they are `System.Text.Json`-serializable. For multi-instance deduplication or persistent triggers, configure a Quartz `JobStore` database backend in your application.
