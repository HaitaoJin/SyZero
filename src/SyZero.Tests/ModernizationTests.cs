using SyZero.DistributedTransaction;
using SyZero.Domain.Entities;
using SyZero.Notifications;
using Xunit;

namespace SyZero.Tests;

public class ModernizationTests
{
    [Fact]
    public async Task Outbox_Lease_AllowsOnlyOneOwnerToClaimMessage()
    {
        var store = new MemoryOutboxStore();
        await store.EnqueueAsync(new OutboxMessage { Id = "message-1", EventName = "order.created" });

        var claims = await Task.WhenAll(
            store.ClaimPendingAsync(1, "worker-a", TimeSpan.FromMinutes(1)),
            store.ClaimPendingAsync(1, "worker-b", TimeSpan.FromMinutes(1)));

        Assert.Equal(1, claims.Sum(x => x.Count));
        Assert.Equal("message-1", claims.SelectMany(x => x).Single().Id);
    }

    [Fact]
    public async Task Inbox_DeduplicatesCompletedMessages_AndReleasesFailedMessages()
    {
        var executor = new InboxExecutor(new MemoryInboxStore());
        var callCount = 0;

        Assert.True(await executor.ExecuteAsync("event-1", "consumer", _ =>
        {
            callCount++;
            return Task.CompletedTask;
        }));
        Assert.False(await executor.ExecuteAsync("event-1", "consumer", _ =>
        {
            callCount++;
            return Task.CompletedTask;
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => executor.ExecuteAsync(
            "event-2", "consumer", _ => throw new InvalidOperationException("retry")));
        Assert.True(await executor.ExecuteAsync("event-2", "consumer", _ =>
        {
            callCount++;
            return Task.CompletedTask;
        }));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task NotificationDispatcher_ReportsSuccessFailureAndUnknownChannels()
    {
        var dispatcher = new NotificationDispatcher([
            new DelegateChannel("ok", _ => Task.CompletedTask),
            new DelegateChannel("failed", _ => throw new InvalidOperationException("channel failed"))
        ]);

        var results = await dispatcher.SendAsync(new NotificationMessage(
            "recipient", "subject", "body", ["ok", "failed", "missing", "OK"]));

        Assert.Collection(results,
            x => { Assert.Equal("ok", x.Channel); Assert.True(x.Succeeded); },
            x => { Assert.Equal("failed", x.Channel); Assert.False(x.Succeeded); Assert.Equal("channel failed", x.Error); },
            x => { Assert.Equal("missing", x.Channel); Assert.False(x.Succeeded); });
    }

    [Fact]
    public void AggregateRoot_CollectsAndClearsDomainEvents()
    {
        var aggregate = new TestAggregate();
        aggregate.Raise("created");

        var domainEvent = Assert.Single(aggregate.DomainEvents);
        Assert.Equal("created", Assert.IsType<TestDomainEvent>(domainEvent).Name);

        aggregate.ClearDomainEvents();
        Assert.Empty(aggregate.DomainEvents);
    }

    private sealed class DelegateChannel(string name, Func<NotificationMessage, Task> send) : INotificationChannel
    {
        public string Name { get; } = name;
        public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default) => send(message);
    }

    private sealed record TestDomainEvent(string Name) : DomainEvent;

    private sealed class TestAggregate : AggregateRoot<Guid>
    {
        public void Raise(string name) => AddDomainEvent(new TestDomainEvent(name));
    }
}
