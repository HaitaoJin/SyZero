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
}

/// <summary>承载 Outbox 表的 DbContext，可独立或合并到业务 DbContext。</summary>
public class OutboxDbContext : DbContext
{
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options) { }

    public DbSet<OutboxMessageEntity> Outbox => Set<OutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OutboxMessageEntity>(b =>
        {
            b.HasIndex(x => x.PublishedAtUtc);
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

    public async Task MarkPublishedAsync(string id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Outbox.FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);
        if (row == null) return;
        row.PublishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(string id, string error, CancellationToken cancellationToken = default)
    {
        var row = await _db.Outbox.FindAsync(new object?[] { id }, cancellationToken).ConfigureAwait(false);
        if (row == null) return;
        row.RetryCount++;
        row.LastError = error;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
