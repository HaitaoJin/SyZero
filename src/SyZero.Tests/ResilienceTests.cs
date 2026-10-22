using System;
using System.Threading.Tasks;
using Polly;
using SyZero.Resilience;
using Xunit;

namespace SyZero.Tests;

public class ResilienceTests
{
    [Fact]
    public async Task Default_pipeline_retries_on_failure_and_eventually_succeeds()
    {
        var pipeline = SyResiliencePipelines.Default(
            timeout: TimeSpan.FromSeconds(5),
            retryCount: 3,
            baseDelay: TimeSpan.FromMilliseconds(1),
            circuitFailureThreshold: 100, // 避免在测试中被熔断
            circuitBreakDuration: TimeSpan.FromSeconds(1));

        var attempts = 0;
        var result = await pipeline.ExecuteAsync(async _ =>
        {
            attempts++;
            if (attempts < 3) throw new InvalidOperationException("transient");
            await Task.Yield();
            return 42;
        });

        Assert.Equal(42, result);
        Assert.Equal(3, attempts);
    }
}
