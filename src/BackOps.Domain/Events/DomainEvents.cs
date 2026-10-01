namespace BackOps.Domain.Events;

using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;

public record TicketPurchaseStartedEvent(
    Guid PurchaseId,
    Guid EventId,
    Guid UserId,
    int Quantity
) : DomainEvent;

public record TicketPurchaseCompletedEvent(
    Guid PurchaseId,
    Guid EventId,
    Guid UserId,
    int Quantity,
    string[] TicketCodes
) : DomainEvent;

public record TicketPurchaseFailedEvent(
    Guid PurchaseId,
    Guid EventId,
    Guid UserId,
    string Reason
) : DomainEvent;

public record InventoryUpdatedEvent(
    Guid EventId,
    int AvailableTickets,
    int ReservedTickets,
    int SoldTickets
) : DomainEvent;

public record VideoJobQueuedEvent(
    Guid JobId,
    string VideoId,
    JobPriority Priority
) : DomainEvent;

public record VideoJobProcessingEvent(
    Guid JobId,
    string WorkerId
) : DomainEvent;

public record VideoJobCompletedEvent(
    Guid JobId,
    TimeSpan ProcessingTime
) : DomainEvent;

public record VideoJobFailedEvent(
    Guid JobId,
    string Error
) : DomainEvent;

public record PaymentCreatedEvent(
    Guid PaymentId,
    IdempotencyKey IdempotencyKey,
    Money Amount
) : DomainEvent;

public record PaymentProcessingEvent(
    Guid PaymentId,
    Guid AttemptId
) : DomainEvent;

public record PaymentAuthorizedEvent(
    Guid PaymentId,
    Guid AttemptId,
    string AuthorizationCode
) : DomainEvent;

public record PaymentCapturedEvent(
    Guid PaymentId,
    Guid AttemptId
) : DomainEvent;

public record PaymentCompletedEvent(
    Guid PaymentId
) : DomainEvent;

public record PaymentFailedEvent(
    Guid PaymentId,
    Guid AttemptId,
    string Reason
) : DomainEvent;

public record PaymentRefundedEvent(
    Guid PaymentId,
    Money Amount
) : DomainEvent;

public record IdempotencyKeyHitEvent(
    IdempotencyKey IdempotencyKey,
    Guid ExistingPaymentId
) : DomainEvent;

public record WebhookSentEvent(
    Guid WebhookId,
    WebhookEventType EventType,
    Guid PaymentId
) : DomainEvent;

public record WebhookFailedEvent(
    Guid WebhookId,
    WebhookEventType EventType,
    string Error
) : DomainEvent;

public record CircuitBreakerStateChangedEvent(
    string CircuitName,
    CircuitBreakerState OldState,
    CircuitBreakerState NewState
) : DomainEvent;

public record FailureInjectedEvent(
    FailureType FailureType,
    double Rate,
    int DurationMs
) : DomainEvent;

public record WorkerStatusChangedEvent(
    string WorkerId,
    WorkerStatus OldStatus,
    WorkerStatus NewStatus
) : DomainEvent;