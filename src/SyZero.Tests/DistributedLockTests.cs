using System;
using System.Threading.Tasks;
using SyZero.DistributedLock;
using Xunit;

namespace SyZero.Tests;

public class DistributedLockTests
{
    [Fact]
    public async Task Local_Acquire_MutualExclusion_WaitTimeoutZero_ReturnsNull()
    {
        var p = new LocalDistributedLockProvider();
        await using var first = await p.AcquireAsync("k1", TimeSpan.FromSeconds(30), TimeSpan.Zero);
        Assert.NotNull(first);

        var second = await p.AcquireAsync("k1", TimeSpan.FromSeconds(30), TimeSpan.Zero);
        Assert.Null(second);
    }

    [Fact]
    public async Task Local_Acquire_DifferentKeys_BothSucceed()
    {
        var p = new LocalDistributedLockProvider();
        await using var a = await p.AcquireAsync("ka", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        await using var b = await p.AcquireAsync("kb", TimeSpan.FromSeconds(10), TimeSpan.Zero);
        Assert.NotNull(a);
        Assert.NotNull(b);
    }

    [Fact]
    public async Task Local_Release_AllowsReacquire()
    {
        var p = new LocalDistributedLockProvider();
        var first = await p.AcquireAsync("k2", TimeSpan.FromSeconds(30), TimeSpan.Zero);
        Assert.NotNull(first);
        await first!.DisposeAsync();

        await using var second = await p.AcquireAsync("k2", TimeSpan.FromSeconds(30), TimeSpan.Zero);
        Assert.NotNull(second);
    }
}
