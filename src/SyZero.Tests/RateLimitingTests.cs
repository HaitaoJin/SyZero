using SyZero.RateLimiting;
using Xunit;

namespace SyZero.Tests;

public class RateLimitingTests
{
    [Fact]
    public async Task LocalRateLimiter_Should_Allow_Within_Limit_And_Reject_Beyond()
    {
        var limiter = new LocalDistributedRateLimiter();
        const string key = "user:1";

        for (int i = 0; i < 3; i++)
        {
            Assert.True(await limiter.TryAcquireAsync(key, permitLimit: 3, window: 60));
        }

        Assert.False(await limiter.TryAcquireAsync(key, permitLimit: 3, window: 60));
    }
}
