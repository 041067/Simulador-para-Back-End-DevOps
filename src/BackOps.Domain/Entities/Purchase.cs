namespace BackOps.Domain.Entities;

using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using BackOps.Domain.Events;

public class Purchase : BaseEntity
{
    public Guid EventId { get; private set; }
    public Guid UserId { get; private set; }
    public int Quantity { get; private set; }
    public Money TotalAmount { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public IdempotencyKey IdempotencyKey { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private readonly List<Ticket> _tickets = [];
    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

    private Purchase() { }

    public Purchase(Guid eventId, Guid userId, int quantity, Money totalAmount, IdempotencyKey idempotencyKey)
    {
        EventId = eventId;
        UserId = userId;
        Quantity = quantity;
        TotalAmount = totalAmount;
        IdempotencyKey = idempotencyKey;
        Status = PurchaseStatus.Pending;
    }

    public void StartProcessing()
    {
        if (Status != PurchaseStatus.Pending)
            throw new InvalidOperationException($"Cannot process purchase with status {Status}");

        Status = PurchaseStatus.Processing;
        ProcessedAt = DateTime.UtcNow;
        UpdateTimestamp();

        AddDomainEvent(new TicketPurchaseStartedEvent(Id, EventId, UserId, Quantity));
    }

    public void Complete(Ticket[] tickets)
    {
        if (Status != PurchaseStatus.Processing)
            throw new InvalidOperationException($"Cannot complete purchase with status {Status}");

        foreach (var ticket in tickets)
        {
            ticket.MarkAsSold();
            _tickets.Add(ticket);
        }

        Status = PurchaseStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();

        AddDomainEvent(new TicketPurchaseCompletedEvent(Id, EventId, UserId, Quantity, tickets.Select(t => t.Code).ToArray()));
    }

    public void Fail(string reason)
    {
        if (Status == PurchaseStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed purchase");

        Status = PurchaseStatus.Failed;
        FailureReason = reason;
        UpdateTimestamp();

        AddDomainEvent(new TicketPurchaseFailedEvent(Id, EventId, UserId, reason));
    }

    public void Cancel()
    {
        if (Status == PurchaseStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed purchase");

        Status = PurchaseStatus.Cancelled;
        UpdateTimestamp();
    }

    public void AddTicket(Ticket ticket) => _tickets.Add(ticket);
}