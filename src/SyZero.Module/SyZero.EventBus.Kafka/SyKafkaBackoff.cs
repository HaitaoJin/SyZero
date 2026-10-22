using System;

namespace SyZero.EventBus.Kafka;

/// <summary>
/// 计算 Kafka 重试退避时长：指数退避 base * 2^(n-1)，叠加 ±20% 抖动，并以 max 封顶。
/// 抽出独立类便于单测（无需启动 Kafka）。
/// </summary>
public static class SyKafkaBackoff
{
    private static readonly Random _rand = new();

    public static TimeSpan Compute(int retryCount, TimeSpan baseDelay, TimeSpan maxDelay)
    {
        if (retryCount <= 0) retryCount = 1;
        var baseMs = baseDelay.TotalMilliseconds;
        var raw = baseMs * Math.Pow(2, retryCount - 1);
        var capped = Math.Min(raw, maxDelay.TotalMilliseconds);
        double jitterFactor;
        lock (_rand)
        {
            jitterFactor = 0.8 + _rand.NextDouble() * 0.4; // [0.8, 1.2)
        }
        return TimeSpan.FromMilliseconds(capped * jitterFactor);
    }
}
