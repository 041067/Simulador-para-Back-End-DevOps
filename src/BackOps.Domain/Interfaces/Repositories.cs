namespace BackOps.Domain.Interfaces;

using BackOps.Domain.Entities;
using BackOps.Domain.Enums;
using BackOps.Domain.ValueObjects;

public interface IEventRepository : IRepository<Event>
{
    Task<Event?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Event>> GetActiveEventsAsync(CancellationToken cancellationToken = default);
    Task<Event?> GetEventWithTicketsAsync(Guid eventId, CancellationToken cancellationToken = default);
}

public interface ITicketRepository : IRepository<Ticket>
{
    Task<IReadOnlyList<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Ticket>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Ticket?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Ticket?> GetByIdempotencyKeyAsync(IdempotencyKey key, CancellationToken cancellationToken = default);
    Task<int> GetAvailableCountAsync(Guid eventId, CancellationToken cancellationToken = default);
}

public interface IPurchaseRepository : IRepository<Purchase>
{
    Task<IReadOnlyList<Purchase>> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Purchase>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Purchase?> GetByIdempotencyKeyAsync(IdempotencyKey key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Purchase>> GetPendingPurchasesAsync(CancellationToken cancellationToken = default);
}

public interface IVideoJobRepository : IRepository<VideoJob>
{
    Task<IReadOnlyList<VideoJob>> GetByStatusAsync(JobStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VideoJob>> GetQueuedJobsAsync(int count, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VideoJob>> GetByWorkerIdAsync(string workerId, CancellationToken cancellationToken = default);
    Task<int> GetQueueDepthAsync(CancellationToken cancellationToken = default);
}

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByIdempotencyKeyAsync(IdempotencyKey key, CancellationToken cancellationToken = default);
    Task<Payment?> GetByExternalReferenceAsync(string reference, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByStatusAsync(PaymentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetPendingPaymentsAsync(CancellationToken cancellationToken = default);
}

public interface IWorkerRepository : IRepository<Worker>
{
    Task<IReadOnlyList<Worker>> GetAvailableWorkersAsync(string type, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Worker>> GetByTypeAsync(string type, CancellationToken cancellationToken = default);
    Task<Worker?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}