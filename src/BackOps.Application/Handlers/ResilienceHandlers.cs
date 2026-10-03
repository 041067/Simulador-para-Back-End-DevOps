namespace BackOps.Application.Handlers;

using BackOps.Application.Commands;
using BackOps.Application.Common;
using BackOps.Application.DTOs;
using BackOps.Application.Queries;
using BackOps.Domain.Interfaces;
using MediatR;

public sealed class ResilienceHandlers :
    IRequestHandler<InjectFailureCommand, Result>,
    IRequestHandler<ClearFailureCommand, Result>,
    IRequestHandler<ClearAllFailuresCommand, Result>,
    IRequestHandler<GetFailureConfigQuery, Result<FailureConfigDto>>,
    IRequestHandler<GetAllFailuresQuery, Result<IReadOnlyList<FailureConfigDto>>>,
    IRequestHandler<GetCircuitBreakerStatusQuery, Result<CircuitBreakerStatusDto>>,
    IRequestHandler<GetAllCircuitBreakersQuery, Result<IReadOnlyList<CircuitBreakerStatusDto>>>,
    IRequestHandler<GetRateLimitInfoQuery, Result<RateLimitInfoDto>>,
    IRequestHandler<ConfigureCircuitBreakerCommand, Result>,
    IRequestHandler<ConfigureRateLimitCommand, Result>
{
    private readonly IFailureInjector _failureInjector;
    private readonly IEnumerable<ICircuitBreaker> _circuitBreakers;
    private readonly IRateLimiter _rateLimiter;

    public ResilienceHandlers(
        IFailureInjector failureInjector,
        IEnumerable<ICircuitBreaker> circuitBreakers,
        IRateLimiter rateLimiter)
    {
        _failureInjector = failureInjector;
        _circuitBreakers = circuitBreakers;
        _rateLimiter = rateLimiter;
    }

    public Task<Result> Handle(InjectFailureCommand r, CancellationToken ct)
    {
        _failureInjector.InjectFailure(r.FailureType, r.Rate, r.DurationMs);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> Handle(ClearFailureCommand r, CancellationToken ct)
    {
        _failureInjector.ClearFailure(r.FailureType);
        return Task.FromResult(Result.Success());
    }

    public Task<Result> Handle(ClearAllFailuresCommand r, CancellationToken ct)
    {
        _failureInjector.ClearAllFailures();
        return Task.FromResult(Result.Success());
    }

    public Task<Result<FailureConfigDto>> Handle(GetFailureConfigQuery r, CancellationToken ct)
    {
        var config = _failureInjector.GetActiveFailure(r.Type);
        return Task.FromResult(config is null
            ? Result<FailureConfigDto>.Failure("Failure configuration not found", "FAILURE_NOT_FOUND")
            : Result<FailureConfigDto>.Success(new FailureConfigDto(
                config.Type, config.Rate, config.DurationMs, config.InjectedAt)));
    }

    public Task<Result<IReadOnlyList<FailureConfigDto>>> Handle(GetAllFailuresQuery r, CancellationToken ct)
    {
        var configs = _failureInjector.GetAllActiveFailures()
            .Select(x => new FailureConfigDto(x.Type, x.Rate, x.DurationMs, x.InjectedAt))
            .ToList();
        return Task.FromResult(Result<IReadOnlyList<FailureConfigDto>>.Success(configs));
    }

    public Task<Result<CircuitBreakerStatusDto>> Handle(GetCircuitBreakerStatusQuery r, CancellationToken ct)
    {
        var breaker = _circuitBreakers.FirstOrDefault(x =>
            string.Equals(x.Name, r.Name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(breaker is null
            ? Result<CircuitBreakerStatusDto>.Failure("Circuit breaker not found", "CIRCUIT_BREAKER_NOT_FOUND")
            : Result<CircuitBreakerStatusDto>.Success(ToDto(breaker)));
    }

    public Task<Result<IReadOnlyList<CircuitBreakerStatusDto>>> Handle(GetAllCircuitBreakersQuery r, CancellationToken ct)
    {
        var result = _circuitBreakers.Select(ToDto).ToList();
        return Task.FromResult(Result<IReadOnlyList<CircuitBreakerStatusDto>>.Success(result));
    }

    public Task<Result<RateLimitInfoDto>> Handle(GetRateLimitInfoQuery r, CancellationToken ct)
    {
        var info = _rateLimiter.GetInfo(r.Key);
        return Task.FromResult(Result<RateLimitInfoDto>.Success(
            new RateLimitInfoDto(info.Limit, info.Remaining, info.ResetAfter, info.IsLimited)));
    }

    public Task<Result> Handle(ConfigureCircuitBreakerCommand r, CancellationToken ct)
    {
        var breaker = _circuitBreakers.FirstOrDefault(x =>
            string.Equals(x.Name, r.Name, StringComparison.OrdinalIgnoreCase));

        if (breaker is null)
            return Task.FromResult(Result.Failure("Circuit breaker not found", "CIRCUIT_BREAKER_NOT_FOUND"));

        return Task.FromResult(Result.Failure(
            "Circuit breaker runtime configuration is not supported by the current implementation",
            "CONFIGURATION_NOT_SUPPORTED"));
    }

    public Task<Result> Handle(ConfigureRateLimitCommand r, CancellationToken ct)
    {
        return Task.FromResult(Result.Failure(
            "Rate limit runtime configuration is not supported by the current implementation",
            "CONFIGURATION_NOT_SUPPORTED"));
    }

    private static CircuitBreakerStatusDto ToDto(ICircuitBreaker x) => new(
        x.Name,
        x.State.ToString().ToLowerInvariant(),
        0,
        0,
        null);
}
