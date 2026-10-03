namespace BackOps.Application.Handlers;

using BackOps.Application.Commands;
using BackOps.Application.Common;
using BackOps.Application.Services;
using MediatR;

public sealed class HighLoadCommandHandlers :
    IRequestHandler<CreateEventCommand, Result<Guid>>,
    IRequestHandler<UpdateEventCommand, Result>,
    IRequestHandler<ActivateEventCommand, Result>,
    IRequestHandler<DeactivateEventCommand, Result>,
    IRequestHandler<CreatePurchaseCommand, Result<Guid>>,
    IRequestHandler<ProcessPurchaseCommand, Result>,
    IRequestHandler<CompletePurchaseCommand, Result>,
    IRequestHandler<FailPurchaseCommand, Result>,
    IRequestHandler<CancelPurchaseCommand, Result>,
    IRequestHandler<CreateVideoJobCommand, Result<Guid>>,
    IRequestHandler<StartVideoJobCommand, Result>,
    IRequestHandler<CompleteVideoJobCommand, Result>,
    IRequestHandler<FailVideoJobCommand, Result>,
    IRequestHandler<RetryVideoJobCommand, Result>,
    IRequestHandler<CancelVideoJobCommand, Result>,
    IRequestHandler<CreateWorkerCommand, Result<Guid>>,
    IRequestHandler<WorkerHeartbeatCommand, Result>,
    IRequestHandler<WorkerStartJobCommand, Result>,
    IRequestHandler<WorkerCompleteJobCommand, Result>,
    IRequestHandler<WorkerFailCommand, Result>,
    IRequestHandler<WorkerStopCommand, Result>
{
    private readonly IHighLoadService _service;

    public HighLoadCommandHandlers(IHighLoadService service) => _service = service;

    public Task<Result<Guid>> Handle(CreateEventCommand r, CancellationToken ct) => _service.CreateEventAsync(r, ct);
    public Task<Result> Handle(UpdateEventCommand r, CancellationToken ct) => _service.UpdateEventAsync(r, ct);
    public Task<Result> Handle(ActivateEventCommand r, CancellationToken ct) => _service.ActivateEventAsync(r.Id, ct);
    public Task<Result> Handle(DeactivateEventCommand r, CancellationToken ct) => _service.DeactivateEventAsync(r.Id, ct);

    public Task<Result<Guid>> Handle(CreatePurchaseCommand r, CancellationToken ct) => _service.CreatePurchaseAsync(r, ct);
    public Task<Result> Handle(ProcessPurchaseCommand r, CancellationToken ct) => _service.ProcessPurchaseAsync(r.PurchaseId, ct);
    public Task<Result> Handle(CompletePurchaseCommand r, CancellationToken ct) => _service.CompletePurchaseAsync(r.PurchaseId, ct);
    public Task<Result> Handle(FailPurchaseCommand r, CancellationToken ct) => _service.FailPurchaseAsync(r.PurchaseId, r.Reason, ct);
    public Task<Result> Handle(CancelPurchaseCommand r, CancellationToken ct) => _service.CancelPurchaseAsync(r.PurchaseId, ct);

    public Task<Result<Guid>> Handle(CreateVideoJobCommand r, CancellationToken ct) => _service.CreateVideoJobAsync(r, ct);
    public Task<Result> Handle(StartVideoJobCommand r, CancellationToken ct) => _service.AssignVideoJobAsync(r.JobId, r.WorkerId, ct);
    public Task<Result> Handle(CompleteVideoJobCommand r, CancellationToken ct) => _service.CompleteVideoJobAsync(r.JobId, ct);
    public Task<Result> Handle(FailVideoJobCommand r, CancellationToken ct) => _service.FailVideoJobAsync(r.JobId, r.Error, ct);
    public Task<Result> Handle(RetryVideoJobCommand r, CancellationToken ct) => _service.RetryVideoJobAsync(r.JobId, ct);
    public Task<Result> Handle(CancelVideoJobCommand r, CancellationToken ct) => _service.CancelVideoJobAsync(r.JobId, ct);

    public Task<Result<Guid>> Handle(CreateWorkerCommand r, CancellationToken ct) => _service.CreateWorkerAsync(r, ct);
    public Task<Result> Handle(WorkerHeartbeatCommand r, CancellationToken ct) => _service.WorkerHeartbeatAsync(r.WorkerId, ct);
    public Task<Result> Handle(WorkerStartJobCommand r, CancellationToken ct) => _service.WorkerStartJobAsync(r.WorkerId, r.JobId, ct);
    public Task<Result> Handle(WorkerCompleteJobCommand r, CancellationToken ct) => _service.WorkerCompleteJobAsync(r.WorkerId, r.Success, ct);
    public Task<Result> Handle(WorkerFailCommand r, CancellationToken ct) => _service.WorkerFailAsync(r.WorkerId, r.Error, ct);
    public Task<Result> Handle(WorkerStopCommand r, CancellationToken ct) => _service.WorkerStopAsync(r.WorkerId, ct);
}
