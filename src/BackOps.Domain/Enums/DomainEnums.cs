namespace BackOps.Domain.Enums;

public enum TicketStatus
{
    Available = 1,
    Reserved = 2,
    Sold = 3,
    Cancelled = 4
}

public enum PurchaseStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum JobStatus
{
    Queued = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum JobPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

public enum PaymentStatus
{
    Created = 1,
    Processing = 2,
    Authorized = 3,
    Captured = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7,
    Refunded = 8
}

public enum PaymentAttemptStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3,
    Timeout = 4
}

public enum BankStatus
{
    Healthy = 1,
    Slow = 2,
    Unavailable = 3,
    Unstable = 4,
    Crashed = 5
}

public enum WebhookEventType
{
    PaymentCreated = 1,
    PaymentProcessing = 2,
    PaymentAuthorized = 3,
    PaymentCompleted = 4,
    PaymentFailed = 5,
    PaymentRefunded = 6
}

public enum WorkerStatus
{
    Idle = 1,
    Processing = 2,
    Failed = 3,
    Stopped = 4
}