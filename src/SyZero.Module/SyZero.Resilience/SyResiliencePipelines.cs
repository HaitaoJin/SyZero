using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System;

namespace SyZero.Resilience;

/// <summary>
/// SyZero 韧性策略快捷构建器，包装 Polly v8 的 <see cref="ResiliencePipelineBuilder"/>。
/// 默认建议组合：超时 → 重试 → 熔断（外层超时兜底，内层重试，最外熔断阻断雪崩）。
/// </summary>
public static class SyResiliencePipelines
{
    /// <summary>
    /// 构建一个包含超时 + 重试 + 熔断的默认管道。
    /// </summary>
    public static ResiliencePipeline Default(
        TimeSpan? timeout = null,
        int retryCount = 3,
        TimeSpan? baseDelay = null,
        int circuitFailureThreshold = 5,
        TimeSpan? circuitBreakDuration = null)
    {
        var builder = new ResiliencePipelineBuilder();

        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30)
        });

        builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = retryCount,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = baseDelay ?? TimeSpan.FromMilliseconds(200)
        });

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = circuitFailureThreshold,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = circuitBreakDuration ?? TimeSpan.FromSeconds(15)
        });

        return builder.Build();
    }
}
