using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using System;

namespace SyZero.Resilience;

/// <summary>
/// SyZero.Resilience 模块的 DI 扩展。
/// </summary>
public static class SyZeroResilienceExtension
{
    /// <summary>
    /// 注册默认 "default" 韧性管道（30s 超时 + 3 次指数退避重试）。
    /// 业务通过 <see cref="ResiliencePipelineProvider{TKey}"/>.GetPipeline("default") 获取。
    /// 自定义管道请直接调用 <c>services.AddResiliencePipeline("name", b => ...)</c>。
    /// </summary>
    public static IServiceCollection AddSyZeroResilience(this IServiceCollection services)
    {
        services.AddResiliencePipeline("default", builder =>
        {
            builder
                .AddTimeout(TimeSpan.FromSeconds(30))
                .AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(200)
                });
        });

        return services;
    }
}
