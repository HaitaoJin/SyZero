using System;
using System.Collections.Generic;

namespace SyZero.Domain.Entities;

/// <summary>领域事件标记；与跨进程集成事件分离。</summary>
public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
}

/// <summary>聚合根，负责暂存当前业务事务内产生的领域事件。</summary>
public abstract class AggregateRoot<TKey> : Entity<TKey>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class AggregateRoot : AggregateRoot<long>
{
    private long _id = SYID.NextId();
    public override long Id
    {
        get => _id;
        set => _id = value == 0 ? SYID.NextId() : value;
    }
}

public interface ISoftDelete { bool IsDeleted { get; set; } }
public interface IHasConcurrencyStamp { string ConcurrencyStamp { get; set; } }
public interface ICreationAudited { DateTime CreatedAtUtc { get; set; } string? CreatedBy { get; set; } }
public interface IModificationAudited { DateTime? ModifiedAtUtc { get; set; } string? ModifiedBy { get; set; } }
