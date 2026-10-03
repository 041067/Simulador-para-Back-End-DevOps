namespace BackOps.Application.Handlers;

using BackOps.Application.Common;
using BackOps.Application.DTOs;
using BackOps.Application.Queries;
using BackOps.Domain.Entities;
using BackOps.Domain.Enums;
using BackOps.Domain.Interfaces;
using MediatR;

public sealed class HighLoadQueryHandlers :
    IRequestHandler<GetEventQuery, Result<EventDto>>,
    IRequestHandler<GetEventsQuery, Result<IReadOnlyList<EventDto>>>,
    IRequestHandler<GetEventInventoryQuery, Result<object>>,
    IRequestHandler<GetPurchaseQuery, Result<PurchaseDto>>,
    IRequestHandler<GetPurchasesByEventQuery, Result<IReadOnlyList<PurchaseDto>>>,
    IRequestHandler<GetPurchasesByUserQuery, Result<IReadOnlyList<PurchaseDto>>>,
    IRequestHandler<GetPendingPurchasesQuery, Result<IReadOnlyList<PurchaseDto>>>,
    IRequestHandler<GetVideoJobQuery, Result<VideoJobDto>>,
    IRequestHandler<GetVideoJobsQuery, Result<IReadOnlyList<VideoJobDto>>>,
    IRequestHandler<GetQueuedVideoJobsQuery, Result<IReadOnlyList<VideoJobDto>>>,
    IRequestHandler<GetVideoJobsByWorkerQuery, Result<IReadOnlyList<VideoJobDto>>>,
    IRequestHandler<GetQueueDepthQuery, Result<int>>,
    IRequestHandler<GetWorkerQuery, Result<WorkerStatusDto>>,
    IRequestHandler<GetWorkersQuery, Result<IReadOnlyList<WorkerStatusDto>>>,
    IRequestHandler<GetAvailableWorkersQuery, Result<IReadOnlyList<WorkerStatusDto>>>,
    IRequestHandler<GetSimulationMetricsQuery, Result<SimulationResultDto>>,
    IRequestHandler<GetQueueMetricsQuery, Result<QueueMetricsDto>>,
    IRequestHandler<GetSystemHealthQuery, Result<HealthCheckDto>>,
    IRequestHandler<GetMetricsSnapshotQuery, Result<MetricsSnapshotDto>>
{
    private readonly IEventRepository _events;
    private readonly IPurchaseRepository _purchases;
    private readonly IVideoJobRepository _videoJobs;
    private readonly IWorkerRepository _workers;
    private readonly IPaymentRepository _payments;
    private readonly IQueueProvider _queue;
    private readonly IEnumerable<ICircuitBreaker> _circuitBreakers;

    public HighLoadQueryHandlers(
        IEventRepository events,
        IPurchaseRepository purchases,
        IVideoJobRepository videoJobs,
        IWorkerRepository workers,
        IPaymentRepository payments,
        IQueueProvider queue,
        IEnumerable<ICircuitBreaker> circuitBreakers)
    {
        _events = events;
        _purchases = purchases;
        _videoJobs = videoJobs;
        _workers = workers;
        _payments = payments;
        _queue = queue;
        _circuitBreakers = circuitBreakers;
    }

    public async Task<Result<EventDto>> Handle(GetEventQuery request, CancellationToken ct)
    {
        var entity = await _events.GetByIdWithDetailsAsync(request.Id, ct);
        return entity is null
            ? Result<EventDto>.Failure("Event not found", "EVENT_NOT_FOUND")
            : Result<EventDto>.Success(ToDto(entity));
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetEventsQuery request, CancellationToken ct)
    {
        var entities = request.ActiveOnly
            ? await _events.GetActiveEventsAsync(ct)
            : await _events.GetAllAsync(ct);

        return Result<IReadOnlyList<EventDto>>.Success(entities.Select(ToDto).ToList());
    }

    public async Task<Result<object>> Handle(GetEventInventoryQuery request, CancellationToken ct)
    {
        var entity = await _events.GetEventWithTicketsAsync(request.EventId, ct);
        if (entity is null)
            return Result<object>.Failure("Event not found", "EVENT_NOT_FOUND");

        return Result<object>.Success(new
        {
            eventId = entity.Id,
            totalCapacity = entity.TotalCapacity,
            availableTickets = entity.AvailableTickets,
            reservedTickets = entity.ReservedTickets,
            soldTickets = entity.SoldTickets
        });
    }

    public async Task<Result<PurchaseDto>> Handle(GetPurchaseQuery request, CancellationToken ct)
    {
        var entity = await _purchases.GetByIdAsync(request.Id, ct);
        return entity is null
            ? Result<PurchaseDto>.Failure("Purchase not found", "PURCHASE_NOT_FOUND")
            : Result<PurchaseDto>.Success(ToDto(entity));
    }

    public async Task<Result<IReadOnlyList<PurchaseDto>>> Handle(GetPurchasesByEventQuery request, CancellationToken ct)
        => Result<IReadOnlyList<PurchaseDto>>.Success(
            (await _purchases.GetByEventIdAsync(request.EventId, ct)).Select(ToDto).ToList());

    public async Task<Result<IReadOnlyList<PurchaseDto>>> Handle(GetPurchasesByUserQuery request, CancellationToken ct)
        => Result<IReadOnlyList<PurchaseDto>>.Success(
            (await _purchases.GetByUserIdAsync(request.UserId, ct)).Select(ToDto).ToList());

    public async Task<Result<IReadOnlyList<PurchaseDto>>> Handle(GetPendingPurchasesQuery request, CancellationToken ct)
        => Result<IReadOnlyList<PurchaseDto>>.Success(
            (await _purchases.GetPendingPurchasesAsync(ct)).Select(ToDto).ToList());

    public async Task<Result<VideoJobDto>> Handle(GetVideoJobQuery request, CancellationToken ct)
    {
        var entity = await _videoJobs.GetByIdAsync(request.Id, ct);
        return entity is null
            ? Result<VideoJobDto>.Failure("Job not found", "JOB_NOT_FOUND")
            : Result<VideoJobDto>.Success(ToDto(entity));
    }

    public async Task<Result<IReadOnlyList<VideoJobDto>>> Handle(GetVideoJobsQuery request, CancellationToken ct)
    {
        var entities = request.Status.HasValue
            ? await _videoJobs.GetByStatusAsync(request.Status.Value, ct)
            : await _videoJobs.GetAllAsync(ct);

        return Result<IReadOnlyList<VideoJobDto>>.Success(entities.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<VideoJobDto>>> Handle(GetQueuedVideoJobsQuery request, CancellationToken ct)
    {
        var count = Math.Clamp(request.Count, 1, 1000);
        return Result<IReadOnlyList<VideoJobDto>>.Success(
            (await _videoJobs.GetQueuedJobsAsync(count, ct)).Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<VideoJobDto>>> Handle(GetVideoJobsByWorkerQuery request, CancellationToken ct)
        => Result<IReadOnlyList<VideoJobDto>>.Success(
            (await _videoJobs.GetByWorkerIdAsync(request.WorkerId, ct)).Select(ToDto).ToList());

    public async Task<Result<int>> Handle(GetQueueDepthQuery request, CancellationToken ct)
        => Result<int>.Success(await _videoJobs.GetQueueDepthAsync(ct));

    public async Task<Result<WorkerStatusDto>> Handle(GetWorkerQuery request, CancellationToken ct)
    {
        var entity = await _workers.GetByIdAsync(request.Id, ct);
        return entity is null
            ? Result<WorkerStatusDto>.Failure("Worker not found", "WORKER_NOT_FOUND")
            : Result<WorkerStatusDto>.Success(ToDto(entity));
    }

    public async Task<Result<IReadOnlyList<WorkerStatusDto>>> Handle(GetWorkersQuery request, CancellationToken ct)
    {
        var entities = string.IsNullOrWhiteSpace(request.Type)
            ? await _workers.GetAllAsync(ct)
            : await _workers.GetByTypeAsync(request.Type, ct);

        return Result<IReadOnlyList<WorkerStatusDto>>.Success(entities.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<WorkerStatusDto>>> Handle(GetAvailableWorkersQuery request, CancellationToken ct)
        => Result<IReadOnlyList<WorkerStatusDto>>.Success(
            (await _workers.GetAvailableWorkersAsync(request.Type, ct)).Select(ToDto).ToList());

    public async Task<Result<SimulationResultDto>> Handle(GetSimulationMetricsQuery request, CancellationToken ct)
    {
        var events = await _events.GetAllAsync(ct);
        var purchases = await _purchases.GetAllAsync(ct);
        var videoJobs = await _videoJobs.GetAllAsync(ct);
        var payments = await _payments.GetAllAsync(ct);
        var workers = await _workers.GetAllAsync(ct);
        var queueDepth = await _videoJobs.GetQueueDepthAsync(ct);

        var totalRequests = request.Scenario switch
        {
            SimulationScenario.TicketSale => purchases.Count,
            SimulationScenario.VideoStreaming => videoJobs.Count,
            SimulationScenario.PaymentProcessing => payments.Count,
            _ => purchases.Count + videoJobs.Count + payments.Count
        };

        var successful = request.Scenario switch
        {
            SimulationScenario.TicketSale => purchases.Count(x => x.Status == PurchaseStatus.Completed),
            SimulationScenario.VideoStreaming => videoJobs.Count(x => x.Status == JobStatus.Completed),
            SimulationScenario.PaymentProcessing => payments.Count(x => x.Status == PaymentStatus.Completed),
            _ => purchases.Count(x => x.Status == PurchaseStatus.Completed)
                 + videoJobs.Count(x => x.Status == JobStatus.Completed)
                 + payments.Count(x => x.Status == PaymentStatus.Completed)
        };

        var failed = request.Scenario switch
        {
            SimulationScenario.TicketSale => purchases.Count(x => x.Status == PurchaseStatus.Failed),
            SimulationScenario.VideoStreaming => videoJobs.Count(x => x.Status == JobStatus.Failed),
            SimulationScenario.PaymentProcessing => payments.Count(x => x.Status == PaymentStatus.Failed),
            _ => purchases.Count(x => x.Status == PurchaseStatus.Failed)
                 + videoJobs.Count(x => x.Status == JobStatus.Failed)
                 + payments.Count(x => x.Status == PaymentStatus.Failed)
        };

        var allCreated = events.Cast<BaseEntity>()
            .Concat(purchases)
            .Concat(videoJobs)
            .Concat(payments)
            .ToList();

        var firstCreated = allCreated.Count == 0
            ? DateTime.UtcNow
            : allCreated.Min(x => x.CreatedAt);

        var duration = DateTime.UtcNow - firstCreated;
        if (duration <= TimeSpan.Zero) duration = TimeSpan.FromSeconds(1);

        var throughput = totalRequests / Math.Max(duration.TotalSeconds, 1);

        return Result<SimulationResultDto>.Success(new SimulationResultDto(
            Guid.NewGuid(),
            request.Scenario,
            totalRequests,
            successful,
            failed,
            (int)queueDepth,
            throughput,
            0,
            0,
            0,
            queueDepth,
            videoJobs.Count(x => x.Status == JobStatus.Queued),
            videoJobs.Count(x => x.Status == JobStatus.Processing),
            videoJobs.Count(x => x.Status == JobStatus.Completed),
            videoJobs.Count(x => x.Status == JobStatus.Failed),
            duration,
            workers.Select(ToDto).ToArray()
        ));
    }

    public async Task<Result<QueueMetricsDto>> Handle(GetQueueMetricsQuery request, CancellationToken ct)
    {
        var pending = await _queue.GetDepthAsync(request.QueueName, ct);
        var processing = 0L;
        var completed = 0L;
        var failed = 0L;

        if (string.Equals(request.QueueName, "backops:videojob", StringComparison.OrdinalIgnoreCase))
        {
            var jobs = await _videoJobs.GetAllAsync(ct);
            processing = jobs.LongCount(x => x.Status == JobStatus.Processing);
            completed = jobs.LongCount(x => x.Status == JobStatus.Completed);
            failed = jobs.LongCount(x => x.Status == JobStatus.Failed);
        }

        return Result<QueueMetricsDto>.Success(new QueueMetricsDto(
            request.QueueName,
            pending,
            processing,
            completed,
            failed,
            0,
            0));
    }

    public async Task<Result<HealthCheckDto>> Handle(GetSystemHealthQuery request, CancellationToken ct)
    {
        var components = new Dictionary<string, string>();
        var healthy = true;

        try
        {
            await _events.GetAllAsync(ct);
            components["postgresql"] = "healthy";
        }
        catch
        {
            components["postgresql"] = "unhealthy";
            healthy = false;
        }

        try
        {
            await _queue.GetDepthAsync("backops:videojob", ct);
            components["redis"] = "healthy";
        }
        catch
        {
            components["redis"] = "unhealthy";
            healthy = false;
        }

        var breakers = _circuitBreakers.ToArray();
        foreach (var breaker in breakers)
            components[$"circuit:{breaker.Name}"] = breaker.State.ToString().ToLowerInvariant();

        return Result<HealthCheckDto>.Success(new HealthCheckDto(
            healthy ? "healthy" : "degraded",
            DateTime.UtcNow,
            components));
    }

    public async Task<Result<MetricsSnapshotDto>> Handle(GetMetricsSnapshotQuery request, CancellationToken ct)
    {
        var purchases = await _purchases.GetAllAsync(ct);
        var videoJobs = await _videoJobs.GetAllAsync(ct);
        var payments = await _payments.GetAllAsync(ct);
        var workers = await _workers.GetAllAsync(ct);
        var queueDepth = await _videoJobs.GetQueueDepthAsync(ct);

        var total = purchases.Count + videoJobs.Count + payments.Count;
        var failed = purchases.Count(x => x.Status == PurchaseStatus.Failed)
            + videoJobs.Count(x => x.Status == JobStatus.Failed)
            + payments.Count(x => x.Status == PaymentStatus.Failed);

        var createdTimes = purchases.Select(x => x.CreatedAt)
            .Concat(videoJobs.Select(x => x.CreatedAt))
            .Concat(payments.Select(x => x.CreatedAt))
            .ToList();

        var durationSeconds = createdTimes.Count == 0
            ? 1
            : Math.Max((DateTime.UtcNow - createdTimes.Min()).TotalSeconds, 1);

        var requestsPerSecond = total / durationSeconds;
        var errorRate = total == 0 ? 0 : (double)failed / total;

        var breakers = _circuitBreakers.Select(x => new CircuitBreakerStatusDto(
            x.Name,
            x.State.ToString().ToLowerInvariant(),
            0,
            0,
            null)).ToArray();

        return Result<MetricsSnapshotDto>.Success(new MetricsSnapshotDto(
            DateTime.UtcNow,
            requestsPerSecond,
            errorRate,
            0,
            0,
            0,
            queueDepth,
            workers.Count(x => x.Status == WorkerStatus.Processing),
            breakers));
    }

    private static EventDto ToDto(Event x) => new(
        x.Id, x.Name, x.Description, x.EventDate, x.TotalCapacity,
        x.AvailableTickets, x.ReservedTickets, x.SoldTickets,
        x.TicketPrice.Amount, x.TicketPrice.Currency, x.IsActive, x.CreatedAt);

    private static TicketDto ToDto(Ticket x) => new(
        x.Id, x.EventId, x.UserId, x.Code, x.Price.Amount, x.Price.Currency,
        x.Status, x.ReservedAt, x.SoldAt, x.CancelledAt);

    private static PurchaseDto ToDto(Purchase x) => new(
        x.Id, x.EventId, x.UserId, x.Quantity, x.TotalAmount.Amount,
        x.TotalAmount.Currency, x.Status, x.FailureReason, x.ProcessedAt,
        x.CompletedAt, x.Tickets.Select(ToDto).ToArray());

    private static VideoJobDto ToDto(VideoJob x) => new(
        x.Id, x.VideoId, x.VideoSizeBytes, x.DurationSeconds, x.Operation,
        x.Priority, x.Status, x.WorkerId, x.ErrorMessage, x.RetryCount,
        x.QueuedAt, x.StartedAt, x.CompletedAt, x.ProcessingTime);

    private static WorkerStatusDto ToDto(Worker x) => new(
        x.Id.ToString(), x.Name, x.Type, x.Status, x.MaxConcurrency,
        x.CurrentJobs, x.TotalJobsProcessed, x.TotalJobsFailed,
        x.Utilization, x.LastHeartbeat);
}
