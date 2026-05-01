using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using SyZero.BackgroundJobs;

namespace SyZero.BackgroundJobs.Quartz;

/// <summary>
/// Quartz IJob 适配：从 JobDataMap 还原 SyZero <see cref="IBackgroundJob{TArgs}"/> 并执行。
/// JobDataMap 必须包含 "JobType"（程序集限定名）与 "ArgsType"、"ArgsJson"。
/// </summary>
[DisallowConcurrentExecution]
public class SyZeroQuartzJobAdapter : IJob
{
    public const string JobTypeKey = "JobType";
    public const string ArgsTypeKey = "ArgsType";
    public const string ArgsJsonKey = "ArgsJson";

    private readonly IServiceProvider _root;
    private readonly ILogger<SyZeroQuartzJobAdapter> _logger;

    public SyZeroQuartzJobAdapter(IServiceProvider root, ILogger<SyZeroQuartzJobAdapter> logger)
    {
        _root = root;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.MergedJobDataMap;
        var jobTypeName = data.GetString(JobTypeKey);
        var argsTypeName = data.GetString(ArgsTypeKey);
        var argsJson = data.GetString(ArgsJsonKey);

        if (string.IsNullOrWhiteSpace(jobTypeName) || string.IsNullOrWhiteSpace(argsTypeName))
        {
            _logger.LogWarning("Quartz 触发的 SyZero 任务缺少 JobType 或 ArgsType，跳过。");
            return;
        }

        var jobType = Type.GetType(jobTypeName!) ?? throw new InvalidOperationException($"找不到任务类型：{jobTypeName}");
        var argsType = Type.GetType(argsTypeName!) ?? throw new InvalidOperationException($"找不到参数类型：{argsTypeName}");
        var args = string.IsNullOrEmpty(argsJson) ? Activator.CreateInstance(argsType) : JsonSerializer.Deserialize(argsJson!, argsType);

        using var scope = _root.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService(jobType);
        var execute = jobType.GetMethod(nameof(IBackgroundJob<object>.ExecuteAsync))
            ?? throw new InvalidOperationException("任务未实现 ExecuteAsync。");
        var task = (Task?)execute.Invoke(job, new[] { args, context.CancellationToken });
        if (task != null) await task.ConfigureAwait(false);
    }
}
