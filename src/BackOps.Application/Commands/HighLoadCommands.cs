namespace BackOps.Application.Commands;

using BackOps.Application.Common;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;

public record CreateEventCommand(
    string Name,
    string Description,
    DateTime EventDate,
    int TotalCapacity,
    decimal TicketPrice,
    string Currency = "BRL"
) : ICommand<Guid>;

public record UpdateEventCommand(
    Guid Id,
    string Name,
    string Description,
    DateTime EventDate,
    decimal TicketPrice,
    string Currency
) : ICommand;

public record ActivateEventCommand(Guid Id) : ICommand;
public record DeactivateEventCommand(Guid Id) : ICommand;

public record CreatePurchaseCommand(
    Guid EventId,
    Guid UserId,
    int Quantity,
    string IdempotencyKey
) : ICommand<Guid>;

public record ProcessPurchaseCommand(Guid PurchaseId) : ICommand;
public record CompletePurchaseCommand(Guid PurchaseId) : ICommand;
public record FailPurchaseCommand(Guid PurchaseId, string Reason) : ICommand;
public record CancelPurchaseCommand(Guid PurchaseId) : ICommand;

public record CreateVideoJobCommand(
    string VideoId,
    long VideoSizeBytes,
    int DurationSeconds,
    string Operation,
    JobPriority Priority
) : ICommand<Guid>;

public record StartVideoJobCommand(Guid JobId, string WorkerId) : ICommand;
public record CompleteVideoJobCommand(Guid JobId) : ICommand;
public record FailVideoJobCommand(Guid JobId, string Error) : ICommand;
public record RetryVideoJobCommand(Guid JobId) : ICommand;
public record CancelVideoJobCommand(Guid JobId) : ICommand;

public record CreateWorkerCommand(
    string Name,
    string Type,
    int MaxConcurrency
) : ICommand<Guid>;

public record WorkerHeartbeatCommand(Guid WorkerId) : ICommand;
public record WorkerStartJobCommand(Guid WorkerId, string JobId) : ICommand;
public record WorkerCompleteJobCommand(Guid WorkerId, bool Success) : ICommand;
public record WorkerFailCommand(Guid WorkerId, string Error) : ICommand;
public record WorkerStopCommand(Guid WorkerId) : ICommand;