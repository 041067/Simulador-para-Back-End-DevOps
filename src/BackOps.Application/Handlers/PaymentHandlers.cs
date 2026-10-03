namespace BackOps.Application.Handlers;

using BackOps.Application.Commands;
using BackOps.Application.Common;
using BackOps.Application.DTOs;
using BackOps.Application.Queries;
using BackOps.Application.Services;
using BackOps.Domain.Enums;
using BackOps.Domain.Interfaces;
using BackOps.Domain.ValueObjects;
using MediatR;

public sealed class PaymentCommandHandlers :
    IRequestHandler<CreatePaymentCommand, Result<Guid>>,
    IRequestHandler<ProcessPaymentCommand, Result>,
    IRequestHandler<AuthorizePaymentCommand, Result>,
    IRequestHandler<CapturePaymentCommand, Result>,
    IRequestHandler<CompletePaymentCommand, Result>,
    IRequestHandler<FailPaymentCommand, Result>,
    IRequestHandler<RefundPaymentCommand, Result>,
    IRequestHandler<CancelPaymentCommand, Result>,
    IRequestHandler<RetryPaymentCommand, Result>,
    IRequestHandler<SendWebhookCommand, Result>,
    IRequestHandler<RetryWebhookCommand, Result>
{
    private readonly IPaymentService _service;

    public PaymentCommandHandlers(IPaymentService service) => _service = service;

    public Task<Result<Guid>> Handle(CreatePaymentCommand r, CancellationToken ct) => _service.CreatePaymentAsync(r, ct);
    public Task<Result> Handle(ProcessPaymentCommand r, CancellationToken ct) => _service.ProcessPaymentAsync(r.PaymentId, ct);
    public Task<Result> Handle(AuthorizePaymentCommand r, CancellationToken ct) => _service.AuthorizePaymentAsync(r.PaymentId, r.AuthorizationCode, ct);
    public Task<Result> Handle(CapturePaymentCommand r, CancellationToken ct) => _service.CapturePaymentAsync(r.PaymentId, ct);
    public Task<Result> Handle(CompletePaymentCommand r, CancellationToken ct) => _service.CompletePaymentAsync(r.PaymentId, ct);
    public Task<Result> Handle(FailPaymentCommand r, CancellationToken ct) => _service.FailPaymentAsync(r.PaymentId, r.Reason, ct);
    public Task<Result> Handle(RefundPaymentCommand r, CancellationToken ct) => _service.RefundPaymentAsync(r.PaymentId, r.Amount, ct);
    public Task<Result> Handle(CancelPaymentCommand r, CancellationToken ct) => _service.CancelPaymentAsync(r.PaymentId, ct);
    public Task<Result> Handle(RetryPaymentCommand r, CancellationToken ct) => _service.RetryPaymentAsync(r.PaymentId, ct);
    public Task<Result> Handle(SendWebhookCommand r, CancellationToken ct) => _service.SendWebhookAsync(r.WebhookId, ct);
    public Task<Result> Handle(RetryWebhookCommand r, CancellationToken ct) => _service.RetryWebhookAsync(r.WebhookId, ct);
}

public sealed class PaymentQueryHandlers :
    IRequestHandler<GetPaymentQuery, Result<PaymentDto>>,
    IRequestHandler<GetPaymentByExternalReferenceQuery, Result<PaymentDto>>,
    IRequestHandler<GetPaymentByIdempotencyKeyQuery, Result<PaymentDto>>,
    IRequestHandler<GetPaymentsQuery, Result<IReadOnlyList<PaymentDto>>>,
    IRequestHandler<GetPendingPaymentsQuery, Result<IReadOnlyList<PaymentDto>>>,
    IRequestHandler<GetWebhookQuery, Result<WebhookDto>>,
    IRequestHandler<GetWebhooksByPaymentQuery, Result<IReadOnlyList<WebhookDto>>>,
    IRequestHandler<GetPendingWebhooksQuery, Result<IReadOnlyList<WebhookDto>>>
{
    private readonly IPaymentRepository _payments;

    public PaymentQueryHandlers(IPaymentRepository payments) => _payments = payments;

    public async Task<Result<PaymentDto>> Handle(GetPaymentQuery r, CancellationToken ct)
        => ToResult(await _payments.GetByIdAsync(r.Id, ct), "Payment not found", "PAYMENT_NOT_FOUND");

    public async Task<Result<PaymentDto>> Handle(GetPaymentByExternalReferenceQuery r, CancellationToken ct)
        => ToResult(await _payments.GetByExternalReferenceAsync(r.Reference, ct), "Payment not found", "PAYMENT_NOT_FOUND");

    public async Task<Result<PaymentDto>> Handle(GetPaymentByIdempotencyKeyQuery r, CancellationToken ct)
    {
        var entity = await _payments.GetByIdempotencyKeyAsync(IdempotencyKey.FromString(r.Key), ct);
        return ToResult(entity, "Payment not found", "PAYMENT_NOT_FOUND");
    }

    public async Task<Result<IReadOnlyList<PaymentDto>>> Handle(GetPaymentsQuery r, CancellationToken ct)
    {
        var entities = r.Status.HasValue
            ? await _payments.GetByStatusAsync(r.Status.Value, ct)
            : await _payments.GetAllAsync(ct);
        return Result<IReadOnlyList<PaymentDto>>.Success(entities.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<PaymentDto>>> Handle(GetPendingPaymentsQuery r, CancellationToken ct)
        => Result<IReadOnlyList<PaymentDto>>.Success(
            (await _payments.GetPendingPaymentsAsync(ct)).Select(ToDto).ToList());

    public async Task<Result<WebhookDto>> Handle(GetWebhookQuery r, CancellationToken ct)
    {
        var webhook = (await _payments.GetAllAsync(ct))
            .SelectMany(x => x.Webhooks)
            .FirstOrDefault(x => x.Id == r.Id);
        return webhook is null
            ? Result<WebhookDto>.Failure("Webhook not found", "WEBHOOK_NOT_FOUND")
            : Result<WebhookDto>.Success(ToDto(webhook));
    }

    public async Task<Result<IReadOnlyList<WebhookDto>>> Handle(GetWebhooksByPaymentQuery r, CancellationToken ct)
    {
        var payment = await _payments.GetByIdAsync(r.PaymentId, ct);
        return payment is null
            ? Result<IReadOnlyList<WebhookDto>>.Failure("Payment not found", "PAYMENT_NOT_FOUND")
            : Result<IReadOnlyList<WebhookDto>>.Success(payment.Webhooks.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<WebhookDto>>> Handle(GetPendingWebhooksQuery r, CancellationToken ct)
    {
        var webhooks = (await _payments.GetAllAsync(ct))
            .SelectMany(x => x.Webhooks)
            .Where(x => !x.IsDelivered)
            .ToList();
        return Result<IReadOnlyList<WebhookDto>>.Success(webhooks.Select(ToDto).ToList());
    }

    private static Result<PaymentDto> ToResult(BackOps.Domain.Entities.Payment? entity, string error, string code)
        => entity is null
            ? Result<PaymentDto>.Failure(error, code)
            : Result<PaymentDto>.Success(ToDto(entity));

    private static PaymentDto ToDto(BackOps.Domain.Entities.Payment x) => new(
        x.Id, x.ExternalReference, x.Amount.Amount, x.Currency, x.Description,
        x.PayerEmail, x.PayerName, x.Status, x.FailureReason, x.AuthorizationCode,
        x.AuthorizedAt, x.CapturedAt, x.CompletedAt, x.FailedAt, x.RetryCount,
        x.Attempts.Select(a => new PaymentAttemptDto(
            a.Id, a.PaymentId, a.Status, a.ErrorMessage, a.AuthorizationCode,
            a.Duration, a.CompletedAt)).ToArray(),
        x.Webhooks.Select(ToDto).ToArray());

    private static WebhookDto ToDto(BackOps.Domain.Entities.Webhook x) => new(
        x.Id, x.PaymentId, x.EventType, x.Payload, x.AttemptCount, x.IsDelivered,
        x.LastError, x.DeliveredAt, x.NextRetryAt);
}
