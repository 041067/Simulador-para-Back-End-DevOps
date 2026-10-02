namespace BackOps.Application.Services;

using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using BackOps.Domain.Exceptions;
using BackOps.Application.Common;
using BackOps.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IMessageBus _messageBus;
    private readonly ICircuitBreaker _bankCircuitBreaker;
    private readonly IRetryPolicy _retryPolicy;
    private readonly IFailureInjector _failureInjector;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IIdempotencyService idempotencyService,
        IMessageBus messageBus,
        ICircuitBreaker bankCircuitBreaker,
        IRetryPolicy retryPolicy,
        IFailureInjector failureInjector,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _idempotencyService = idempotencyService;
        _messageBus = messageBus;
        _bankCircuitBreaker = bankCircuitBreaker;
        _retryPolicy = retryPolicy;
        _failureInjector = failureInjector;
        _logger = logger;
    }

    public async Task<Result<Guid>> CreatePaymentAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = IdempotencyKey.FromString(command.IdempotencyKey);

        var idempotencyResult = await _idempotencyService.ExecuteAsync(idempotencyKey, async ct =>
        {
            var payment = new Payment(
                Money.FromDecimal(command.Amount, command.Currency),
                command.Description,
                command.PayerEmail,
                command.PayerName,
                command.CardToken,
                idempotencyKey
            );

            await _paymentRepository.AddAsync(payment, ct);

            _logger.LogInformation("Created payment {PaymentId} with idempotency key {IdempotencyKey}", payment.Id, idempotencyKey);

            return payment.Id;
        }, cancellationToken);

        if (idempotencyResult.IsNew)
            return Result<Guid>.Success(idempotencyResult.Result);

        return idempotencyResult.ExistingEntityId is { } existingPaymentId
            ? Result<Guid>.Success(existingPaymentId)
            : Result<Guid>.Failure("Idempotency operation completed without a payment ID", "IDEMPOTENCY_RESULT_INVALID");
    }

    public async Task<Result> ProcessPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        if (!payment.CanRetry() && payment.Status != PaymentStatus.Created)
            return Result.Failure($"Cannot process payment in status {payment.Status}", "INVALID_STATUS");

        payment.StartProcessing();
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        try
        {
            await _bankCircuitBreaker.ExecuteAsync(async ct =>
            {
                var bankAvailable = await CheckBankAvailabilityAsync(ct);
                if (!bankAvailable)
                    throw new BankUnavailableException(BankStatus.Unavailable);

                await SimulateBankProcessingAsync(payment, ct);
            }, cancellationToken);

            var attempt = payment.AddAttempt(PaymentAttemptStatus.Success, authorizationCode: $"AUTH-{Guid.NewGuid().ToString("N")[..8].ToUpper()}");
            payment.Authorize(attempt.AuthorizationCode!);
        }
        catch (CircuitBreakerOpenException)
        {
            payment.AddAttempt(PaymentAttemptStatus.Failed, "Circuit breaker open");
            payment.Fail("Bank circuit breaker is open");
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            return Result.Failure("Payment service temporarily unavailable", "CIRCUIT_BREAKER_OPEN");
        }
        catch (BankUnavailableException ex)
        {
            payment.AddAttempt(PaymentAttemptStatus.Failed, $"Bank {ex.BankStatus}");
            payment.Fail($"Bank is {ex.BankStatus}");
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            return Result.Failure("Bank unavailable", "BANK_UNAVAILABLE");
        }
        catch (Exception ex)
        {
            payment.AddAttempt(PaymentAttemptStatus.Failed, ex.Message);
            payment.Fail(ex.Message);
            await _paymentRepository.UpdateAsync(payment, cancellationToken);
            return Result.Failure(ex.Message, "PROCESSING_ERROR");
        }

        await _paymentRepository.UpdateAsync(payment, cancellationToken);
        await SendWebhookForPaymentAsync(payment, WebhookEventType.PaymentProcessing, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> AuthorizePaymentAsync(Guid paymentId, string authorizationCode, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        payment.Authorize(authorizationCode);
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        await SendWebhookForPaymentAsync(payment, WebhookEventType.PaymentAuthorized, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CapturePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        payment.Capture();
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CompletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        payment.Complete();
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        await SendWebhookForPaymentAsync(payment, WebhookEventType.PaymentCompleted, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> FailPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        payment.Fail(reason);
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        await SendWebhookForPaymentAsync(payment, WebhookEventType.PaymentFailed, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RefundPaymentAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        payment.Refund(Money.FromDecimal(amount, payment.Currency));
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        await SendWebhookForPaymentAsync(payment, WebhookEventType.PaymentRefunded, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CancelPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        payment.Cancel();
        await _paymentRepository.UpdateAsync(payment, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RetryPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment == null)
            return Result.Failure("Payment not found", "PAYMENT_NOT_FOUND");

        if (!payment.CanRetry())
            return Result.Failure("Payment cannot be retried", "CANNOT_RETRY");

        payment.IncrementRetry();
        return await ProcessPaymentAsync(paymentId, cancellationToken);
    }

    public async Task<Result> SendWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetAllAsync(cancellationToken);
        var webhook = payments.SelectMany(p => p.Webhooks).FirstOrDefault(w => w.Id == webhookId);
        if (webhook == null)
            return Result.Failure("Webhook not found", "WEBHOOK_NOT_FOUND");

        // Simulate webhook delivery
        await SimulateWebhookDeliveryAsync(webhook, cancellationToken);

        webhook.MarkAsDelivered();
        await _paymentRepository.UpdateAsync(payments.First(p => p.Webhooks.Any(w => w.Id == webhookId)), cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RetryWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetAllAsync(cancellationToken);
        var webhook = payments.SelectMany(p => p.Webhooks).FirstOrDefault(w => w.Id == webhookId);
        if (webhook == null)
            return Result.Failure("Webhook not found", "WEBHOOK_NOT_FOUND");

        if (!webhook.CanRetry())
            return Result.Failure("Webhook cannot be retried", "MAX_RETRIES_EXCEEDED");

        webhook.RecordAttempt();
        await _paymentRepository.UpdateAsync(payments.First(p => p.Webhooks.Any(w => w.Id == webhookId)), cancellationToken);

        return await SendWebhookAsync(webhookId, cancellationToken);
    }

    private async Task<bool> CheckBankAvailabilityAsync(CancellationToken cancellationToken)
    {
        var failures = _failureInjector.GetAllActiveFailures();

        if (failures.Any(f => f.Type == FailureType.DatabaseUnavailable))
            return false;

        if (failures.Any(f => f.Type == FailureType.DatabaseLatency))
            await Task.Delay(failures.First(f => f.Type == FailureType.DatabaseLatency).DurationMs ?? 5000, cancellationToken);

        if (failures.Any(f => f.Type == FailureType.RandomErrors))
        {
            var random = Random.Shared.NextDouble();
            if (random < failures.First(f => f.Type == FailureType.RandomErrors).Rate)
                return false;
        }

        return true;
    }

    private async Task SimulateBankProcessingAsync(Payment payment, CancellationToken cancellationToken)
    {
        var processingTime = Random.Shared.Next(100, 2000);
        await Task.Delay(processingTime, cancellationToken);

        // Simulate bank response
        var random = Random.Shared.NextDouble();
        if (random < 0.05) // 5% failure rate
            throw new BankUnavailableException(BankStatus.Unstable);
    }

    private async Task SendWebhookForPaymentAsync(Payment payment, WebhookEventType eventType, CancellationToken cancellationToken)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            paymentId = payment.Id,
            externalReference = payment.ExternalReference,
            amount = payment.Amount.Amount,
            currency = payment.Currency,
            status = payment.Status.ToString(),
            timestamp = DateTime.UtcNow
        });

        var webhook = payment.AddWebhook(eventType, payload);

        _logger.LogInformation("Queued webhook {WebhookId} for payment {PaymentId} event {EventType}", webhook.Id, payment.Id, eventType);

        await _messageBus.PublishAsync(new WebhookDeliveryMessage(webhook.Id, payment.Id, eventType, payload), cancellationToken);
    }

    private async Task SimulateWebhookDeliveryAsync(Webhook webhook, CancellationToken cancellationToken)
    {
        var failures = _failureInjector.GetAllActiveFailures();

        if (failures.Any(f => f.Type == FailureType.NetworkTimeout))
            await Task.Delay(failures.First(f => f.Type == FailureType.NetworkTimeout).DurationMs ?? 5000, cancellationToken);

        if (failures.Any(f => f.Type == FailureType.RandomErrors))
        {
            var random = Random.Shared.NextDouble();
            if (random < failures.First(f => f.Type == FailureType.RandomErrors).Rate)
                throw new Exception("Network error");
        }

        await Task.Delay(Random.Shared.Next(50, 500), cancellationToken);
    }

    private record WebhookDeliveryMessage(Guid WebhookId, Guid PaymentId, WebhookEventType EventType, string Payload);
}
