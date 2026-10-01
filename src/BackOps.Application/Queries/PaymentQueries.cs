namespace BackOps.Application.Queries;

using BackOps.Application.Common;
using BackOps.Application.DTOs;
using BackOps.Domain.Enums;
using MediatR;

public record GetPaymentQuery(Guid Id) : IQuery<PaymentDto>;
public record GetPaymentByExternalReferenceQuery(string Reference) : IQuery<PaymentDto>;
public record GetPaymentByIdempotencyKeyQuery(string Key) : IQuery<PaymentDto>;
public record GetPaymentsQuery(PaymentStatus? Status = null) : IQuery<IReadOnlyList<PaymentDto>>;
public record GetPendingPaymentsQuery : IQuery<IReadOnlyList<PaymentDto>>;

public record GetWebhookQuery(Guid Id) : IQuery<WebhookDto>;
public record GetWebhooksByPaymentQuery(Guid PaymentId) : IQuery<IReadOnlyList<WebhookDto>>;
public record GetPendingWebhooksQuery : IQuery<IReadOnlyList<WebhookDto>>;

public record GetFailureConfigQuery(FailureType Type) : IQuery<FailureConfigDto>;
public record GetAllFailuresQuery : IQuery<IReadOnlyList<FailureConfigDto>>;

public record GetCircuitBreakerStatusQuery(string Name) : IQuery<CircuitBreakerStatusDto>;
public record GetAllCircuitBreakersQuery : IQuery<IReadOnlyList<CircuitBreakerStatusDto>>;

public record GetRateLimitInfoQuery(string Key) : IQuery<RateLimitInfoDto>;

public record FailureConfigDto(
    FailureType Type,
    double Rate,
    int? DurationMs,
    DateTime InjectedAt
);

public record RateLimitInfoDto(
    int Limit,
    int Remaining,
    TimeSpan ResetAfter,
    bool IsLimited
);