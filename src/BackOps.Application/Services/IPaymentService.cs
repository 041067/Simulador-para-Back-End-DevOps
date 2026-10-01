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

public interface IPaymentService
{
    Task<Result<Guid>> CreatePaymentAsync(CreatePaymentCommand command, CancellationToken cancellationToken = default);
    Task<Result> ProcessPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result> AuthorizePaymentAsync(Guid paymentId, string authorizationCode, CancellationToken cancellationToken = default);
    Task<Result> CapturePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result> CompletePaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result> FailPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default);
    Task<Result> RefundPaymentAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken = default);
    Task<Result> CancelPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Result> RetryPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<Result> SendWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);
    Task<Result> RetryWebhookAsync(Guid webhookId, CancellationToken cancellationToken = default);
}