namespace BackOps.Application.Commands;

using BackOps.Application.Common;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using MediatR;

public record CreatePaymentCommand(
    decimal Amount,
    string Currency,
    string Description,
    string PayerEmail,
    string PayerName,
    string CardToken,
    string IdempotencyKey
) : ICommand<Guid>;

public record ProcessPaymentCommand(Guid PaymentId) : ICommand;
public record AuthorizePaymentCommand(Guid PaymentId, string AuthorizationCode) : ICommand;
public record CapturePaymentCommand(Guid PaymentId) : ICommand;
public record CompletePaymentCommand(Guid PaymentId) : ICommand;
public record FailPaymentCommand(Guid PaymentId, string Reason) : ICommand;
public record RefundPaymentCommand(Guid PaymentId, decimal Amount) : ICommand;
public record CancelPaymentCommand(Guid PaymentId) : ICommand;
public record RetryPaymentCommand(Guid PaymentId) : ICommand;

public record SendWebhookCommand(Guid WebhookId) : ICommand;
public record RetryWebhookCommand(Guid WebhookId) : ICommand;

public record InjectFailureCommand(
    FailureType FailureType,
    double Rate,
    int? DurationMs = null
) : ICommand;

public record ClearFailureCommand(FailureType FailureType) : ICommand;
public record ClearAllFailuresCommand() : ICommand;

public record ConfigureCircuitBreakerCommand(
    string Name,
    int FailureThreshold,
    TimeSpan Timeout,
    int SamplingDuration
) : ICommand;

public record ConfigureRateLimitCommand(
    string Key,
    int Limit,
    TimeSpan Window
) : ICommand;