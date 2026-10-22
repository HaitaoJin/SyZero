using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using SyZero.BackgroundJobs;

namespace SyZero.BackgroundJobs.Quartz;

public static class SyZeroBackgroundJobsQuartzExtension
{
    /// <summary>
    /// 注册 Quartz 调度器与 SyZero 适配 IJob。
    /// </summary>
    public static IServiceCollection AddSyZeroBackgroundJobsQuartz(this IServiceCollection services, Action<IServiceCollectionQuartzConfigurator>? configure = null)
    {
        services.AddQuartz(q =>
        {
            configure?.Invoke(q);
        });
        services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);
        return services;
    }

    /// <summary>
    /// 注册一条 Cron 调度，把指定 SyZero 任务（<typeparamref name="TJob"/> + <typeparamref name="TArgs"/>）按 cron 触发。
    /// </summary>
    public static IServiceCollectionQuartzConfigurator AddSyZeroCron<TJob, TArgs>(this IServiceCollectionQuartzConfigurator q, string cron, TArgs args, string? name = null)
        where TJob : IBackgroundJob<TArgs>
    {
        var jobKey = new JobKey(name ?? $"{typeof(TJob).FullName}:{Guid.NewGuid():N}");
        q.AddJob<SyZeroQuartzJobAdapter>(opts => opts
            .WithIdentity(jobKey)
            .UsingJobData(SyZeroQuartzJobAdapter.JobTypeKey, typeof(TJob).AssemblyQualifiedName!)
            .UsingJobData(SyZeroQuartzJobAdapter.ArgsTypeKey, typeof(TArgs).AssemblyQualifiedName!)
            .UsingJobData(SyZeroQuartzJobAdapter.ArgsJsonKey, JsonSerializer.Serialize(args)));
        q.AddTrigger(t => t.ForJob(jobKey).WithCronSchedule(cron));
        return q;
    }
}
