namespace BackOps.Application.DTOs;

using BackOps.Domain.Enums;
using BackOps.Domain.ValueObjects;

public record EventDto(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    int TotalCapacity,
    int AvailableTickets,
    int ReservedTickets,
    int SoldTickets,
    decimal TicketPrice,
    string Currency,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateEventDto(
    string Name,
    string Description,
    DateTime EventDate,
    int TotalCapacity,
    decimal TicketPrice,
    string Currency = "BRL"
);

public record TicketDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    string Code,
    decimal Price,
    string Currency,
    TicketStatus Status,
    DateTime? ReservedAt,
    DateTime? SoldAt,
    DateTime? CancelledAt
);

public record PurchaseDto(
    Guid Id,
    Guid EventId,
    Guid UserId,
    int Quantity,
    decimal TotalAmount,
    string Currency,
    PurchaseStatus Status,
    string? FailureReason,
    DateTime? ProcessedAt,
    DateTime? CompletedAt,
    TicketDto[] Tickets
);

public record CreatePurchaseDto(
    Guid EventId,
    Guid UserId,
    int Quantity,
    string IdempotencyKey
);

public record VideoJobDto(
    Guid Id,
    string VideoId,
    long VideoSizeBytes,
    int DurationSeconds,
    string Operation,
    JobPriority Priority,
    JobStatus Status,
    string? WorkerId,
    string? ErrorMessage,
    int RetryCount,
    DateTime? QueuedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    TimeSpan? ProcessingTime
);

public record CreateVideoJobDto(
    string VideoId,
    long VideoSizeBytes,
    int DurationSeconds,
    string Operation = "TRANSCODE",
    JobPriority Priority = JobPriority.Normal
);

public record PaymentDto(
    Guid Id,
    string ExternalReference,
    decimal Amount,
    string Currency,
    string Description,
    string PayerEmail,
    string PayerName,
    PaymentStatus Status,
    string? FailureReason,
    string? AuthorizationCode,
    DateTime? AuthorizedAt,
    DateTime? CapturedAt,
    DateTime? CompletedAt,
    DateTime? FailedAt,
    int RetryCount,
    PaymentAttemptDto[] Attempts,
    WebhookDto[] Webhooks
);

public record PaymentAttemptDto(
    Guid Id,
    Guid PaymentId,
    PaymentAttemptStatus Status,
    string? ErrorMessage,
    string? AuthorizationCode,
    TimeSpan? Duration,
    DateTime? CompletedAt
);

public record WebhookDto(
    Guid Id,
    Guid PaymentId,
    WebhookEventType EventType,
    string Payload,
    int AttemptCount,
    bool IsDelivered,
    string? LastError,
    DateTime? DeliveredAt,
    DateTime? NextRetryAt
);

public record CreatePaymentDto(
    decimal Amount,
    string Currency,
    string Description,
    string PayerEmail,
    string PayerName,
    string CardToken,
    string IdempotencyKey
);

public record SimulationConfigDto(
    SimulationScenario Scenario,
    int Users,
    int RequestsPerUser,
    int Workers,
    QueueProvider QueueProvider,
    bool EnableFailureInjection,
    double FailureRate,
    int FailureDurationMs
);

public record SimulationResultDto(
    Guid SimulationId,
    SimulationScenario Scenario,
    int TotalRequests,
    int SuccessfulRequests,
    int FailedRequests,
    int QueuedRequests,
    double Throughput,
    double AvgLatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    long QueueDepth,
    int PendingJobs,
    int ProcessingJobs,
    int CompletedJobs,
    int FailedJobs,
    TimeSpan Duration,
    WorkerStatusDto[] Workers
);

public record WorkerStatusDto(
    string Id,
    string Name,
    string Type,
    WorkerStatus Status,
    int MaxConcurrency,
    int CurrentJobs,
    int TotalJobsProcessed,
    int TotalJobsFailed,
    double Utilization,
    DateTime? LastHeartbeat
);

public record QueueMetricsDto(
    string QueueName,
    long Pending,
    long Processing,
    long Completed,
    long Failed,
    double Throughput,
    double AvgProcessingTimeMs
);

public record HealthCheckDto(
    string Status,
    DateTime Timestamp,
    Dictionary<string, string> Components
);

public record MetricsSnapshotDto(
    DateTime Timestamp,
    double RequestsPerSecond,
    double ErrorRate,
    double P50LatencyMs,
    double P95LatencyMs,
    double P99LatencyMs,
    long QueueDepth,
    int ActiveWorkers,
    CircuitBreakerStatusDto[] CircuitBreakers
);

public record CircuitBreakerStatusDto(
    string Name,
    string State,
    int FailureCount,
    int SuccessCount,
    DateTime? LastStateChange
);