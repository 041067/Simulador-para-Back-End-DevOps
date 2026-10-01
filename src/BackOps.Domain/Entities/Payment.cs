namespace BackOps.Domain.Entities;

using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using BackOps.Domain.Events;

public class Payment : BaseEntity
{
    public string ExternalReference { get; private set; } = string.Empty;
    public Money Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public string Description { get; private set; } = string.Empty;
    public string PayerEmail { get; private set; } = string.Empty;
    public string PayerName { get; private set; } = string.Empty;
    public string CardToken { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public IdempotencyKey IdempotencyKey { get; private set; }
    public string? FailureReason { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public DateTime? AuthorizedAt { get; private set; }
    public DateTime? CapturedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;

    private readonly List<PaymentAttempt> _attempts = [];
    public IReadOnlyCollection<PaymentAttempt> Attempts => _attempts.AsReadOnly();

    private readonly List<Webhook> _webhooks = [];
    public IReadOnlyCollection<Webhook> Webhooks => _webhooks.AsReadOnly();

    private Payment() { }

    public Payment(Money amount, string description, string payerEmail, string payerName, string cardToken, IdempotencyKey idempotencyKey)
    {
        Amount = amount;
        Currency = amount.Currency;
        Description = description;
        PayerEmail = payerEmail;
        PayerName = payerName;
        CardToken = cardToken;
        IdempotencyKey = idempotencyKey;
        ExternalReference = $"PAY-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
        Status = PaymentStatus.Created;

        AddDomainEvent(new PaymentCreatedEvent(Id, idempotencyKey, amount));
    }

    public void StartProcessing()
    {
        if (Status != PaymentStatus.Created && Status != PaymentStatus.Failed)
            throw new InvalidOperationException($"Cannot process payment with status {Status}");

        Status = PaymentStatus.Processing;
        UpdateTimestamp();

        AddDomainEvent(new PaymentProcessingEvent(Id, Guid.NewGuid()));
    }

    public void Authorize(string authorizationCode)
    {
        if (Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot authorize payment with status {Status}");

        AuthorizationCode = authorizationCode;
        Status = PaymentStatus.Authorized;
        AuthorizedAt = DateTime.UtcNow;
        UpdateTimestamp();

        AddDomainEvent(new PaymentAuthorizedEvent(Id, Guid.NewGuid(), authorizationCode));
    }

    public void Capture()
    {
        if (Status != PaymentStatus.Authorized)
            throw new InvalidOperationException($"Cannot capture payment with status {Status}");

        Status = PaymentStatus.Captured;
        CapturedAt = DateTime.UtcNow;
        UpdateTimestamp();

        AddDomainEvent(new PaymentCapturedEvent(Id, Guid.NewGuid()));
    }

    public void Complete()
    {
        if (Status != PaymentStatus.Captured)
            throw new InvalidOperationException($"Cannot complete payment with status {Status}");

        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();

        AddDomainEvent(new PaymentCompletedEvent(Id));
    }

    public void Fail(string reason)
    {
        if (Status == PaymentStatus.Completed)
            throw new InvalidOperationException("Cannot fail a completed payment");

        Status = PaymentStatus.Failed;
        FailureReason = reason;
        FailedAt = DateTime.UtcNow;
        RetryCount++;
        UpdateTimestamp();

        AddDomainEvent(new PaymentFailedEvent(Id, Guid.NewGuid(), reason));
    }

    public void Refund(Money amount)
    {
        if (Status != PaymentStatus.Completed)
            throw new InvalidOperationException($"Cannot refund payment with status {Status}");

        Status = PaymentStatus.Refunded;
        UpdateTimestamp();

        AddDomainEvent(new PaymentRefundedEvent(Id, amount));
    }

    public void Cancel()
    {
        if (Status == PaymentStatus.Completed || Status == PaymentStatus.Refunded)
            throw new InvalidOperationException($"Cannot cancel payment with status {Status}");

        Status = PaymentStatus.Cancelled;
        UpdateTimestamp();
    }

    public bool CanRetry() => RetryCount < MaxRetries && (Status == PaymentStatus.Failed || Status == PaymentStatus.Processing);

    public void IncrementRetry() => RetryCount++;

    public PaymentAttempt AddAttempt(PaymentAttemptStatus status, string? errorMessage = null, string? authorizationCode = null)
    {
        var attempt = new PaymentAttempt(Id, status, errorMessage, authorizationCode);
        _attempts.Add(attempt);
        return attempt;
    }

    public Webhook AddWebhook(WebhookEventType eventType, string payload)
    {
        var webhook = new Webhook(Id, eventType, payload);
        _webhooks.Add(webhook);
        return webhook;
    }
}