namespace BackOps.Domain.Entities;

using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using BackOps.Domain.Events;

public class Ticket : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public Money Price { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public IdempotencyKey IdempotencyKey { get; private set; }
    public DateTime? ReservedAt { get; private set; }
    public DateTime? SoldAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private Ticket() { }

    public Ticket(Guid eventId, Guid userId, Money price, IdempotencyKey idempotencyKey)
    {
        EventId = eventId;
        UserId = userId;
        Price = price;
        IdempotencyKey = idempotencyKey;
        Code = GenerateTicketCode();
        Status = TicketStatus.Available;
    }

    private static string GenerateTicketCode()
    {
        return $"TK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }

    public void Reserve()
    {
        if (Status != TicketStatus.Available)
            throw new InvalidOperationException($"Cannot reserve ticket with status {Status}");

        Status = TicketStatus.Reserved;
        ReservedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsSold()
    {
        if (Status != TicketStatus.Reserved)
            throw new InvalidOperationException($"Cannot sell ticket with status {Status}");

        Status = TicketStatus.Sold;
        SoldAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status == TicketStatus.Sold || Status == TicketStatus.Cancelled)
            throw new InvalidOperationException($"Cannot cancel ticket with status {Status}");

        Status = TicketStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
}