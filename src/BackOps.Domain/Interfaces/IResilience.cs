namespace BackOps.Domain.Interfaces;

using BackOps.Domain.Enums;
using BackOps.Domain.ValueObjects;

public interface ICircuitBreaker
{
    string Name { get; }
    CircuitBreakerState State { get; }
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    void Reset();
    event Action<CircuitBreakerState, CircuitBreakerState>? OnStateChanged;
}

public interface IRetryPolicy
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}

public interface IRateLimiter
{
    Task<bool> TryAcquireAsync(string key, int permits = 1, CancellationToken cancellationToken = default);
    Task ReleaseAsync(string key, int permits = 1, CancellationToken cancellationToken = default);
    RateLimitInfo GetInfo(string key);
}

public record RateLimitInfo(int Limit, int Remaining, TimeSpan ResetAfter, bool IsLimited);

public interface IIdempotencyService
{
    Task<IdempotencyResult<T>> ExecuteAsync<T>(IdempotencyKey key, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
    Task<IdempotencyResult> ExecuteAsync(IdempotencyKey key, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(IdempotencyKey key, CancellationToken cancellationToken = default);
}

public record IdempotencyResult<T>(bool IsNew, T? Result, Guid? ExistingEntityId);
public record IdempotencyResult(bool IsNew, Guid? ExistingEntityId);

public interface IFailureInjector
{
    void InjectFailure(FailureType type, double rate, int? durationMs = null);
    void ClearFailure(FailureType type);
    void ClearAllFailures();
    FailureConfig? GetActiveFailure(FailureType type);
    IReadOnlyCollection<FailureConfig> GetAllActiveFailures();
}

public record FailureConfig(FailureType Type, double Rate, int? DurationMs, DateTime InjectedAt);