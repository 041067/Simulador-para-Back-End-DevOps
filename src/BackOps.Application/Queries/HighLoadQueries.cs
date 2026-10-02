namespace BackOps.Application.Queries;

using BackOps.Application.Common;
using BackOps.Application.DTOs;
using BackOps.Domain.Enums;

public record GetEventQuery(Guid Id) : IQuery<EventDto>;
public record GetEventsQuery(bool ActiveOnly = true) : IQuery<IReadOnlyList<EventDto>>;
public record GetEventInventoryQuery(Guid EventId) : IQuery<object>;

public record GetPurchaseQuery(Guid Id) : IQuery<PurchaseDto>;
public record GetPurchasesByEventQuery(Guid EventId) : IQuery<IReadOnlyList<PurchaseDto>>;
public record GetPurchasesByUserQuery(Guid UserId) : IQuery<IReadOnlyList<PurchaseDto>>;
public record GetPendingPurchasesQuery : IQuery<IReadOnlyList<PurchaseDto>>;

public record GetVideoJobQuery(Guid Id) : IQuery<VideoJobDto>;
public record GetVideoJobsQuery(JobStatus? Status = null) : IQuery<IReadOnlyList<VideoJobDto>>;
public record GetQueuedVideoJobsQuery(int Count = 100) : IQuery<IReadOnlyList<VideoJobDto>>;
public record GetVideoJobsByWorkerQuery(string WorkerId) : IQuery<IReadOnlyList<VideoJobDto>>;
public record GetQueueDepthQuery : IQuery<int>;

public record GetWorkerQuery(Guid Id) : IQuery<WorkerStatusDto>;
public record GetWorkersQuery(string? Type = null) : IQuery<IReadOnlyList<WorkerStatusDto>>;
public record GetAvailableWorkersQuery(string Type) : IQuery<IReadOnlyList<WorkerStatusDto>>;

public record GetSimulationMetricsQuery(SimulationScenario Scenario) : IQuery<SimulationResultDto>;
public record GetQueueMetricsQuery(string QueueName) : IQuery<QueueMetricsDto>;
public record GetSystemHealthQuery : IQuery<HealthCheckDto>;
public record GetMetricsSnapshotQuery : IQuery<MetricsSnapshotDto>;