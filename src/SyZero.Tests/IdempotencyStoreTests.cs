using System;
using System.Threading.Tasks;
using SyZero.Idempotency;
using Xunit;

namespace SyZero.Tests;

public class IdempotencyStoreTests
{
    [Fact]
    public async Task TryReserve_TwiceForSameKey_FailsSecondTime()
    {
        var store = new MemoryIdempotencyStore();
        var first = await store.TryReserveAsync("k1", TimeSpan.FromMinutes(1));
        var second = await store.TryReserveAsync("k1", TimeSpan.FromMinutes(1));
        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task SaveAndGet_ReturnsEntry()
    {
        var store = new MemoryIdempotencyStore();
        await store.TryReserveAsync("k2", TimeSpan.FromMinutes(1));
        await store.SaveAsync("k2", new IdempotencyEntry { StatusCode = 200, ContentType = "application/json", Body = new byte[] { 1, 2, 3 } }, TimeSpan.FromMinutes(1));

        var entry = await store.GetAsync("k2");
        Assert.NotNull(entry);
        Assert.Equal(200, entry!.StatusCode);
        Assert.Equal(3, entry.Body!.Length);
    }
}
