namespace BackOps.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
    Guid EventId { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public Guid EventId { get; init; } = Guid.NewGuid();
}