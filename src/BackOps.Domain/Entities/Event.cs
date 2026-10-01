namespace BackOps.Domain.Entities;

using BackOps.Domain.ValueObjects;
using BackOps.Domain.Events;
using BackOps.Domain.Enums;
using BackOps.Domain.Exceptions;

public class Event : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime EventDate { get; private set; }
    public int TotalCapacity { get; private set; }
    public int AvailableTickets { get; private set; }
    public int ReservedTickets { get; private set; }
    public int SoldTickets { get; private set; }
    public Money TicketPrice { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Ticket> _tickets = [];
    public IReadOnlyCollection<Ticket> Tickets => _tickets.AsReadOnly();

    private readonly List<Purchase> _purchases = [];
    public IReadOnlyCollection<Purchase> Purchases => _purchases.AsReadOnly();

    private Event() { }

    public Event(string name, string description, DateTime eventDate, int totalCapacity, Money ticketPrice)
    {
        Name = name;
        Description = description;
        EventDate = eventDate;
        TotalCapacity = totalCapacity;
        AvailableTickets = totalCapacity;
        ReservedTickets = 0;
        SoldTickets = 0;
        TicketPrice = ticketPrice;
        IsActive = true;
    }

    public void UpdateDetails(string name, string description, DateTime eventDate, Money ticketPrice)
    {
        Name = name;
        Description = description;
        EventDate = eventDate;
        TicketPrice = ticketPrice;
        UpdateTimestamp();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public bool CanReserve(int quantity)
    {
        return IsActive && AvailableTickets >= quantity;
    }

    public Ticket[] ReserveTickets(int quantity, Guid userId, IdempotencyKey idempotencyKey)
    {
        if (!CanReserve(quantity))
            throw new InsufficientInventoryException(quantity, AvailableTickets);

        var tickets = new Ticket[quantity];
        for (int i = 0; i < quantity; i++)
        {
            var ticket = new Ticket(Id, userId, TicketPrice, idempotencyKey);
            ticket.Reserve();
            _tickets.Add(ticket);
            tickets[i] = ticket;
        }

        AvailableTickets -= quantity;
        ReservedTickets += quantity;
        UpdateTimestamp();

        AddDomainEvent(new InventoryUpdatedEvent(Id, AvailableTickets, ReservedTickets, SoldTickets));
        return tickets;
    }

    public void ConfirmReservation(Ticket ticket)
    {
        if (ticket.EventId != Id)
            throw new InvalidOperationException("Ticket does not belong to this event");

        ticket.MarkAsSold();
        ReservedTickets--;
        SoldTickets++;
        AvailableTickets--;
        UpdateTimestamp();

        AddDomainEvent(new InventoryUpdatedEvent(Id, AvailableTickets, ReservedTickets, SoldTickets));
    }

    public void CancelReservation(Ticket ticket)
    {
        if (ticket.EventId != Id)
            throw new InvalidOperationException("Ticket does not belong to this event");

        ticket.Cancel();
        ReservedTickets--;
        AvailableTickets++;
        UpdateTimestamp();

        AddDomainEvent(new InventoryUpdatedEvent(Id, AvailableTickets, ReservedTickets, SoldTickets));
    }

    public void AddPurchase(Purchase purchase) => _purchases.Add(purchase);
}