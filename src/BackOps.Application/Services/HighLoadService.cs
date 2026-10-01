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

public class HighLoadService : IHighLoadService
{
    private readonly IEventRepository _eventRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IPurchaseRepository _purchaseRepository;
    private readonly IVideoJobRepository _videoJobRepository;
    private readonly IWorkerRepository _workerRepository;
    private readonly IIdempotencyService _idempotencyService;
    private readonly IMessageBus _messageBus;
    private readonly IMediator _mediator;
    private readonly ILogger<HighLoadService> _logger;

    public HighLoadService(
        IEventRepository eventRepository,
        ITicketRepository ticketRepository,
        IPurchaseRepository purchaseRepository,
        IVideoJobRepository videoJobRepository,
        IWorkerRepository workerRepository,
        IIdempotencyService idempotencyService,
        IMessageBus messageBus,
        IMediator mediator,
        ILogger<HighLoadService> logger)
    {
        _eventRepository = eventRepository;
        _ticketRepository = ticketRepository;
        _purchaseRepository = purchaseRepository;
        _videoJobRepository = videoJobRepository;
        _workerRepository = workerRepository;
        _idempotencyService = idempotencyService;
        _messageBus = messageBus;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Result<Guid>> CreateEventAsync(CreateEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = new Event(
            command.Name,
            command.Description,
            command.EventDate,
            command.TotalCapacity,
            Money.FromDecimal(command.TicketPrice, command.Currency)
        );

        await _eventRepository.AddAsync(@event, cancellationToken);
        _logger.LogInformation("Created event {EventId} with capacity {Capacity}", @event.Id, @event.TotalCapacity);

        return Result<Guid>.Success(@event.Id);
    }

    public async Task<Result> UpdateEventAsync(UpdateEventCommand command, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(command.Id, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found", "EVENT_NOT_FOUND");

        @event.UpdateDetails(command.Name, command.Description, command.EventDate, Money.FromDecimal(command.TicketPrice, command.Currency));
        await _eventRepository.UpdateAsync(@event, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> ActivateEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found", "EVENT_NOT_FOUND");

        @event.Activate();
        await _eventRepository.UpdateAsync(@event, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeactivateEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(id, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found", "EVENT_NOT_FOUND");

        @event.Deactivate();
        await _eventRepository.UpdateAsync(@event, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<Guid>> CreatePurchaseAsync(CreatePurchaseCommand command, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = IdempotencyKey.FromString(command.IdempotencyKey);

        return await _idempotencyService.ExecuteAsync(idempotencyKey, async ct =>
        {
            var @event = await _eventRepository.GetByIdAsync(command.EventId, ct);
            if (@event == null)
                throw new InvalidOperationException("Event not found");

            if (!@event.IsActive)
                throw new InvalidOperationException("Event is not active");

            var totalAmount = @event.TicketPrice.Multiply(command.Quantity);

            var purchase = new Purchase(
                command.EventId,
                command.UserId,
                command.Quantity,
                totalAmount,
                idempotencyKey
            );

            await _purchaseRepository.AddAsync(purchase, ct);

            _logger.LogInformation("Created purchase {PurchaseId} for event {EventId}", purchase.Id, command.EventId);

            return Result<Guid>.Success(purchase.Id);
        }, cancellationToken);
    }

    public async Task<Result> ProcessPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase == null)
            return Result.Failure("Purchase not found", "PURCHASE_NOT_FOUND");

        purchase.StartProcessing();

        var @event = await _eventRepository.GetEventWithTicketsAsync(purchase.EventId, cancellationToken);
        if (@event == null)
        {
            purchase.Fail("Event not found");
            await _purchaseRepository.UpdateAsync(purchase, cancellationToken);
            return Result.Failure("Event not found", "EVENT_NOT_FOUND");
        }

        try
        {
            var tickets = @event.ReserveTickets(purchase.Quantity, purchase.UserId, purchase.IdempotencyKey);

            foreach (var ticket in tickets)
            {
                purchase.AddTicket(ticket);
                await _ticketRepository.AddAsync(ticket, cancellationToken);
            }

            await _eventRepository.UpdateAsync(@event, cancellationToken);
            await _purchaseRepository.UpdateAsync(purchase, cancellationToken);

            _logger.LogInformation("Processing purchase {PurchaseId} - reserved {Count} tickets", purchaseId, tickets.Length);
        }
        catch (InsufficientInventoryException ex)
        {
            purchase.Fail($"Insufficient inventory: {ex.Message}");
            await _purchaseRepository.UpdateAsync(purchase, cancellationToken);
            return Result.Failure(ex.Message, "INSUFFICIENT_INVENTORY");
        }

        return Result.Success();
    }

    public async Task<Result> CompletePurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase == null)
            return Result.Failure("Purchase not found", "PURCHASE_NOT_FOUND");

        var @event = await _eventRepository.GetEventWithTicketsAsync(purchase.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found", "EVENT_NOT_FOUND");

        purchase.Complete(purchase.Tickets.ToArray());

        foreach (var ticket in purchase.Tickets)
        {
            @event.ConfirmReservation(ticket);
            await _ticketRepository.UpdateAsync(ticket, cancellationToken);
        }

        await _eventRepository.UpdateAsync(@event, cancellationToken);
        await _purchaseRepository.UpdateAsync(purchase, cancellationToken);

        _logger.LogInformation("Completed purchase {PurchaseId}", purchaseId);

        return Result.Success();
    }

    public async Task<Result> FailPurchaseAsync(Guid purchaseId, string reason, CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase == null)
            return Result.Failure("Purchase not found", "PURCHASE_NOT_FOUND");

        var @event = await _eventRepository.GetEventWithTicketsAsync(purchase.EventId, cancellationToken);
        if (@event != null)
        {
            foreach (var ticket in purchase.Tickets)
            {
                @event.CancelReservation(ticket);
                await _ticketRepository.UpdateAsync(ticket, cancellationToken);
            }
            await _eventRepository.UpdateAsync(@event, cancellationToken);
        }

        purchase.Fail(reason);
        await _purchaseRepository.UpdateAsync(purchase, cancellationToken);

        _logger.LogWarning("Failed purchase {PurchaseId}: {Reason}", purchaseId, reason);

        return Result.Success();
    }

    public async Task<Result> CancelPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken = default)
    {
        var purchase = await _purchaseRepository.GetByIdAsync(purchaseId, cancellationToken);
        if (purchase == null)
            return Result.Failure("Purchase not found", "PURCHASE_NOT_FOUND");

        purchase.Cancel();
        await _purchaseRepository.UpdateAsync(purchase, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateVideoJobAsync(CreateVideoJobCommand command, CancellationToken cancellationToken = default)
    {
        var job = new VideoJob(
            command.VideoId,
            command.VideoSizeBytes,
            command.DurationSeconds,
            command.Operation,
            command.Priority
        );

        await _videoJobRepository.AddAsync(job, cancellationToken);

        await _messageBus.PublishAsync(job, cancellationToken);

        _logger.LogInformation("Created video job {JobId} for video {VideoId}", job.Id, command.VideoId);

        return Result<Guid>.Success(job.Id);
    }

    public async Task<Result> AssignVideoJobAsync(Guid jobId, string workerId, CancellationToken cancellationToken = default)
    {
        var job = await _videoJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return Result.Failure("Job not found", "JOB_NOT_FOUND");

        job.AssignToWorker(workerId);
        await _videoJobRepository.UpdateAsync(job, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CompleteVideoJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _videoJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return Result.Failure("Job not found", "JOB_NOT_FOUND");

        job.Complete();
        await _videoJobRepository.UpdateAsync(job, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> FailVideoJobAsync(Guid jobId, string error, CancellationToken cancellationToken = default)
    {
        var job = await _videoJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return Result.Failure("Job not found", "JOB_NOT_FOUND");

        job.Fail(error);
        await _videoJobRepository.UpdateAsync(job, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RetryVideoJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _videoJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return Result.Failure("Job not found", "JOB_NOT_FOUND");

        if (!job.CanRetry())
            return Result.Failure("Job cannot be retried", "CANNOT_RETRY");

        job.Fail("Retrying");
        await _videoJobRepository.UpdateAsync(job, cancellationToken);

        await _messageBus.PublishAsync(job, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> CancelVideoJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _videoJobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return Result.Failure("Job not found", "JOB_NOT_FOUND");

        job.Cancel();
        await _videoJobRepository.UpdateAsync(job, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<Guid>> CreateWorkerAsync(CreateWorkerCommand command, CancellationToken cancellationToken = default)
    {
        var worker = new Worker(command.Name, command.Type, command.MaxConcurrency);
        await _workerRepository.AddAsync(worker, cancellationToken);

        return Result<Guid>.Success(worker.Id);
    }

    public async Task<Result> WorkerHeartbeatAsync(Guid workerId, CancellationToken cancellationToken = default)
    {
        var worker = await _workerRepository.GetByIdAsync(workerId, cancellationToken);
        if (worker == null)
            return Result.Failure("Worker not found", "WORKER_NOT_FOUND");

        worker.Heartbeat();
        await _workerRepository.UpdateAsync(worker, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> WorkerStartJobAsync(Guid workerId, string jobId, CancellationToken cancellationToken = default)
    {
        var worker = await _workerRepository.GetByIdAsync(workerId, cancellationToken);
        if (worker == null)
            return Result.Failure("Worker not found", "WORKER_NOT_FOUND");

        worker.StartJob(jobId);
        await _workerRepository.UpdateAsync(worker, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> WorkerCompleteJobAsync(Guid workerId, bool success, CancellationToken cancellationToken = default)
    {
        var worker = await _workerRepository.GetByIdAsync(workerId, cancellationToken);
        if (worker == null)
            return Result.Failure("Worker not found", "WORKER_NOT_FOUND");

        worker.CompleteJob(success);
        await _workerRepository.UpdateAsync(worker, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> WorkerFailAsync(Guid workerId, string error, CancellationToken cancellationToken = default)
    {
        var worker = await _workerRepository.GetByIdAsync(workerId, cancellationToken);
        if (worker == null)
            return Result.Failure("Worker not found", "WORKER_NOT_FOUND");

        worker.Fail(error);
        await _workerRepository.UpdateAsync(worker, cancellationToken);

        return Result.Success();
    }

    public async Task<Result> WorkerStopAsync(Guid workerId, CancellationToken cancellationToken = default)
    {
        var worker = await _workerRepository.GetByIdAsync(workerId, cancellationToken);
        if (worker == null)
            return Result.Failure("Worker not found", "WORKER_NOT_FOUND");

        worker.Stop();
        await _workerRepository.UpdateAsync(worker, cancellationToken);

        return Result.Success();
    }
}