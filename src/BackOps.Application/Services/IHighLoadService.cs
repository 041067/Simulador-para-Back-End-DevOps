namespace BackOps.Application.Services;

using BackOps.Domain.Interfaces;
using BackOps.Domain.Entities;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using BackOps.Domain.Exceptions;
using BackOps.Application.Common;
using BackOps.Application.Commands;

public interface IHighLoadService
{
    Task<Result<Guid>> CreateEventAsync(CreateEventCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateEventAsync(UpdateEventCommand command, CancellationToken cancellationToken = default);
    Task<Result> ActivateEventAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result> DeactivateEventAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreatePurchaseAsync(CreatePurchaseCommand command, CancellationToken cancellationToken = default);
    Task<Result> ProcessPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default);
    Task<Result> CompletePurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default);
    Task<Result> FailPurchaseAsync(Guid purchaseId, string reason, CancellationToken cancellationToken = default);
    Task<Result> CancelPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateVideoJobAsync(CreateVideoJobCommand command, CancellationToken cancellationToken = default);
    Task<Result> AssignVideoJobAsync(Guid jobId, string workerId, CancellationToken cancellationToken = default);
    Task<Result> CompleteVideoJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<Result> FailVideoJobAsync(Guid jobId, string error, CancellationToken cancellationToken = default);
    Task<Result> RetryVideoJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<Result> CancelVideoJobAsync(Guid jobId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateWorkerAsync(CreateWorkerCommand command, CancellationToken cancellationToken = default);
    Task<Result> WorkerHeartbeatAsync(Guid workerId, CancellationToken cancellationToken = default);
    Task<Result> WorkerStartJobAsync(Guid workerId, string jobId, CancellationToken cancellationToken = default);
    Task<Result> WorkerCompleteJobAsync(Guid workerId, bool success, CancellationToken cancellationToken = default);
    Task<Result> WorkerFailAsync(Guid workerId, string error, CancellationToken cancellationToken = default);
    Task<Result> WorkerStopAsync(Guid workerId, CancellationToken cancellationToken = default);
}
