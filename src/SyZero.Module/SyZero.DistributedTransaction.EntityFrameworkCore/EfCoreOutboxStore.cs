using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SyZero.Dependency;

namespace SyZero.DistributedTransaction.EntityFrameworkCore;

/// <summary>EF Core 持久化的 Outbox 行实体。</summary>
[Table("SyOutboxMessages")]
public sealed class OutboxMessageEntity
{
    [Key, MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required, MaxLength(256)]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public int RetryCount { get; set; }

    [MaxLength(2048)]
    public string? LastError { get; set; }
    [MaxLength(256)]
    public string? LockOwner { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
}

[Table("SyInboxMessages")]
public sealed class InboxMessageEntity
{
    [MaxLength(128)] public string MessageId { get; set; } = string.Empty;
    [MaxLength(256)] public string Consumer { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? LockedUntilUtc { get; set; }
    public int RetryCount { get; set; }
    [MaxLength(2048)] public string? LastError { get; set; }
}

/// <summary>承载 Outbox 表的 DbContext，可独立或合并到业务 DbContext。</summary>
public class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    public DbSet<OutboxMessageEntity> Outbox => Set<OutboxMessageEntity>();
    public DbSet<InboxMessageEntity> Inbox => Set<InboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OutboxMessageEntity>(b =>
        {
            b.HasIndex(x => x.PublishedAtUtc);
            b.HasIndex(x => new { x.PublishedAtUtc, x.LockedUntilUtc, x.CreatedAtUtc });
        });
        modelBuilder.Entity<InboxMessageEntity>(b =>
        {
            b.HasKey(x => new { x.MessageId, x.Consumer });
            b.HasIndex(x => x.ProcessedAtUtc);
        });
    }
}

/// <summary>基于 EF Core 的 <see cref="IOutboxStore"/> 实现。</summary>
public sealed class EfCoreOutboxStore : IOutboxStore, IScopedDependency
{
    private readonly OutboxDbContext _db;
    public EfCoreOutboxStore(OutboxDbContext db) => _db = db;

    public async Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _db.Outbox.AddAsync(new OutboxMessageEntity
        {
            Id = message.Id,
            EventName = message.EventName,
            PayloadJson = message.PayloadJson,
            CreatedAtUtc = message.CreatedAtUtc,
            PublishedAtUtc = message.PublishedAtUtc,
            RetryCount = message.RetryCount,
            LastError = message.LastError
        }, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OutboxMessage>> FetchPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Outbox
            .AsNoTracking()
            .Where(x => x.PublishedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows.Select(r => new OutboxMessage
        {
            Id = r.Id,
            EventName = r.EventName,
            PayloadJson = r.PayloadJson,
            CreatedAtUtc = r.CreatedAtUtc,
            PublishedAtUtc = r.PublishedAtUtc,
            RetryCount = r.RetryCount,
            LastError = r.LastError
        }).ToList();
    }

    public async Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(int batchSize, string lockOwner, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var ids = await _db.Outbox.AsNoTracking()
            .Where(x => x.PublishedAtUtc == null && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .OrderBy(x => x.CreatedAtUtc).Select(x => x.Id).Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (ids.Count == 0) return Array.Empty<OutboxMessage>();
        await _db.Outbox
            .Where(x => ids.Contains(x.Id) && x.PublishedAtUtc == null && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.LockOwner, lockOwner)
                .SetProperty(x => x.LockedUntilUtc, now.Add(lease)), cancellationToken)
            .ConfigureAwait(false);
        var claimed = await _db.Outbox.AsNoTracking()
            .Where(x => x.LockOwner == lockOwner && x.PublishedAtUtc == null)
            .OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return claimed.Select(r => new OutboxMessage
        {
            Id = r.Id, EventName = r.EventName, PayloadJson = r.PayloadJson,
            CreatedAtUtc = r.CreatedAtUtc, PublishedAtUtc = r.PublishedAtUtc,
            RetryCount = r.RetryCount, LastError = r.LastError,
            LockOwner = r.LockOwner, LockedUntilUtc = r.LockedUntilUtc
        }).ToList();
    }

    public async Task MarkPublishedAsync(string id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Outbox.FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);
        if (row == null) return;
        row.PublishedAtUtc = DateTime.UtcNow;
        row.LockOwner = null;
        row.LockedUntilUtc = null;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(string id, string error, CancellationToken cancellationToken = default)
    {
        var row = await _db.Outbox.FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);
        if (row == null) return;
        row.RetryCount++;
        row.LastError = error;
        row.LockOwner = null;
        row.LockedUntilUtc = null;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class EfCoreInboxStore(OutboxDbContext db) : IInboxStore, IScopedDependency
{
    public async Task<bool> TryBeginAsync(string messageId, string consumer, TimeSpan lease, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var changed = await db.Inbox.Where(x => x.MessageId == messageId && x.Consumer == consumer &&
                x.ProcessedAtUtc == null && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockedUntilUtc, now.Add(lease)), cancellationToken)
            .ConfigureAwait(false);
        if (changed > 0) return true;
        if (await db.Inbox.AsNoTracking().AnyAsync(x => x.MessageId == messageId && x.Consumer == consumer, cancellationToken).ConfigureAwait(false))
            return false;
        db.Inbox.Add(new InboxMessageEntity
        {
            MessageId = messageId, Consumer = consumer, ReceivedAtUtc = now, LockedUntilUtc = now.Add(lease)
        });
        try { await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return true; }
        catch (DbUpdateException) { db.ChangeTracker.Clear(); return false; }
    }

    public Task CompleteAsync(string messageId, string consumer, CancellationToken cancellationToken = default)
        => db.Inbox.Where(x => x.MessageId == messageId && x.Consumer == consumer)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ProcessedAtUtc, DateTime.UtcNow).SetProperty(x => x.LockedUntilUtc, (DateTime?)null), cancellationToken);

    public Task FailAsync(string messageId, string consumer, string error, CancellationToken cancellationToken = default)
        => db.Inbox.Where(x => x.MessageId == messageId && x.Consumer == consumer)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RetryCount, x => x.RetryCount + 1).SetProperty(x => x.LastError, error).SetProperty(x => x.LockedUntilUtc, (DateTime?)null), cancellationToken);
}
