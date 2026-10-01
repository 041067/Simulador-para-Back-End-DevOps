namespace BackOps.Domain.Entities;

using BackOps.Domain.Enums;

public class PaymentAttempt : BaseEntity
{
    public Guid PaymentId { get; private set; }
    public PaymentAttemptStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? AuthorizationCode { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private PaymentAttempt() { }

    public PaymentAttempt(Guid paymentId, PaymentAttemptStatus status, string? errorMessage = null, string? authorizationCode = null)
    {
        PaymentId = paymentId;
        Status = status;
        ErrorMessage = errorMessage;
        AuthorizationCode = authorizationCode;
    }

    public void Complete(TimeSpan duration)
    {
        Status = PaymentAttemptStatus.Success;
        Duration = duration;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Fail(string errorMessage, TimeSpan duration)
    {
        Status = PaymentAttemptStatus.Failed;
        ErrorMessage = errorMessage;
        Duration = duration;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Timeout(TimeSpan duration)
    {
        Status = PaymentAttemptStatus.Timeout;
        Duration = duration;
        CompletedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }
}