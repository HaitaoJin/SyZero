using System.Threading.Tasks;
using SyZero.DistributedTransaction;
using Xunit;

namespace SyZero.Tests;

public class OutboxStoreTests
{
    [Fact]
    public async Task EnqueueAndFetchPending_Works()
    {
        var store = new MemoryOutboxStore();
        await store.EnqueueAsync(new OutboxMessage { EventName = "OrderCreated", PayloadJson = "{}" });
        await store.EnqueueAsync(new OutboxMessage { EventName = "PaymentDone", PayloadJson = "{}" });

        var pending = await store.FetchPendingAsync(10);
        Assert.Equal(2, pending.Count);

        await store.MarkPublishedAsync(pending[0].Id);
        var pending2 = await store.FetchPendingAsync(10);
        Assert.Single(pending2);
    }

    [Fact]
    public async Task MarkFailed_IncrementsRetryCount()
    {
        var store = new MemoryOutboxStore();
        var msg = new OutboxMessage { EventName = "X" };
        await store.EnqueueAsync(msg);
        await store.MarkFailedAsync(msg.Id, "boom");
        var pending = await store.FetchPendingAsync(10);
        Assert.Single(pending);
        Assert.Equal(1, pending[0].RetryCount);
        Assert.Equal("boom", pending[0].LastError);
    }
}
