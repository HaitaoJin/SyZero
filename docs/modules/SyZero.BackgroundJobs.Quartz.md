# SyZero.BackgroundJobs.Quartz

为 `SyZero.BackgroundJobs` 提供基于 Quartz 的 Cron / 调度后端，复用统一的 `IBackgroundJob<TArgs>` 抽象。

## 📦 安装

```bash
dotnet add package SyZero.BackgroundJobs.Quartz
```

## ✨ 特性

- ⏰ **Cron 表达式调度**：用 `AddSyZeroCron<TJob, TArgs>(cron, args)` 一行注册定时任务。
- 🧩 **复用业务任务**：业务任务仍实现 `IBackgroundJob<TArgs>`，无需为 Quartz 写适配。
- 🔁 **`[DisallowConcurrentExecution]`**：默认避免同一 Job 重叠运行。

## 🚀 快速开始

```csharp
builder.Services.AddSyZeroBackgroundJobs(builder.Configuration);
builder.Services.AddBackgroundJob<DailyReportJob>();

builder.Services.AddSyZeroBackgroundJobsQuartz(q =>
{
    q.AddSyZeroCron<DailyReportJob, DailyReportArgs>("0 0 2 * * ?", new DailyReportArgs { Date = "today" });
});
```

## 🧩 注意

- `Args` 通过 JSON 序列化进 `JobDataMap`，请确保参数类型可被 `System.Text.Json` 序列化。
- 当前只覆盖**进程内**调度；若需跨实例去重 / 持久化触发器，需配置 Quartz `JobStore` 数据库后端（业务侧自行配置）。
