using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.DistributedTransaction;

/// <summary>记录消费端处理状态，用于实现至少一次投递下的幂等消费。</summary>
public sealed class InboxMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string Consumer { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}

public interface IInboxStore
{
    /// <summary>尝试取得消息处理权；已完成或仍在租约内时返回 false。</summary>
    Task<bool> TryBeginAsync(string messageId, string consumer, TimeSpan lease, CancellationToken cancellationToken = default);
    Task CompleteAsync(string messageId, string consumer, CancellationToken cancellationToken = default);
    Task FailAsync(string messageId, string consumer, string error, CancellationToken cancellationToken = default);
}

public sealed class MemoryInboxStore : IInboxStore
{
    private readonly ConcurrentDictionary<string, InboxMessage> _messages = new(StringComparer.Ordinal);

    public Task<bool> TryBeginAsync(string messageId, string consumer, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var key = Key(messageId, consumer);
        var now = DateTime.UtcNow;
        var acquired = false;
        _messages.AddOrUpdate(key,
            _ => { acquired = true; return New(messageId, consumer, now, lease); },
            (_, current) =>
            {
                lock (current)
                {
                    if (current.ProcessedAtUtc != null || current.LockedUntilUtc > now) return current;
                    current.LockedUntilUtc = now.Add(lease);
                    acquired = true;
                    return current;
                }
            });
        return Task.FromResult(acquired);
    }

    public Task CompleteAsync(string messageId, string consumer, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(Key(messageId, consumer), out var item))
        {
            item.ProcessedAtUtc = DateTime.UtcNow;
            item.LockedUntilUtc = null;
        }
        return Task.CompletedTask;
    }

    public Task FailAsync(string messageId, string consumer, string error, CancellationToken cancellationToken = default)
    {
        if (_messages.TryGetValue(Key(messageId, consumer), out var item))
        {
            item.RetryCount++;
            item.LastError = error;
            item.LockedUntilUtc = null;
        }
        return Task.CompletedTask;
    }

    private static string Key(string id, string consumer) => $"{consumer}\n{id}";
    private static InboxMessage New(string id, string consumer, DateTime now, TimeSpan lease) => new()
    {
        MessageId = id, Consumer = consumer, ReceivedAtUtc = now, LockedUntilUtc = now.Add(lease)
    };
}

/// <summary>围绕事件处理器执行 Inbox 去重、租约与失败释放。</summary>
public sealed class InboxExecutor(IInboxStore store)
{
    public async Task<bool> ExecuteAsync(
        string messageId,
        string consumer,
        Func<CancellationToken, Task> handler,
        TimeSpan? lease = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);
        ArgumentNullException.ThrowIfNull(handler);
        if (!await store.TryBeginAsync(messageId, consumer, lease ?? TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false))
            return false;
        try
        {
            await handler(cancellationToken).ConfigureAwait(false);
            await store.CompleteAsync(messageId, consumer, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            await store.FailAsync(messageId, consumer, ex.Message, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
