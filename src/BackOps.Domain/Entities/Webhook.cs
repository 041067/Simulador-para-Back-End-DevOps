namespace BackOps.Domain.Entities;

using BackOps.Domain.Enums;

public class Webhook : BaseEntity
{
    public Guid PaymentId { get; private set; }
    public WebhookEventType EventType { get; private set; }
    public string Payload { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; } = 5;
    public bool IsDelivered { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    private Webhook() { }

    public Webhook(Guid paymentId, WebhookEventType eventType, string payload)
    {
        PaymentId = paymentId;
        EventType = eventType;
        Payload = payload;
    }

    public void MarkAsDelivered()
    {
        IsDelivered = true;
        DeliveredAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void RecordAttempt(string? error = null)
    {
        AttemptCount++;
        LastError = error;
        NextRetryAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, AttemptCount) * 30);
        UpdateTimestamp();
    }

    public bool CanRetry() => !IsDelivered && AttemptCount < MaxAttempts;
}