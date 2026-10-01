namespace BackOps.Infrastructure.Resilience;

using BackOps.Domain.Interfaces;
using BackOps.Domain.Enums;
using BackOps.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

public class InMemoryFailureInjector : IFailureInjector
{
    private readonly Dictionary<FailureType, FailureConfig> _failures = new();
    private readonly object _lock = new();
    private readonly ILogger<InMemoryFailureInjector> _logger;

    public InMemoryFailureInjector(ILogger<InMemoryFailureInjector> logger)
    {
        _logger = logger;
    }

    public void InjectFailure(FailureType type, double rate, int? durationMs = null)
    {
        lock (_lock)
        {
            _failures[type] = new FailureConfig(type, rate, durationMs, DateTime.UtcNow);
            _logger.LogWarning("Failure injected: {Type} at rate {Rate} for {Duration}ms", type, rate, durationMs);
        }
    }

    public void ClearFailure(FailureType type)
    {
        lock (_lock)
        {
            if (_failures.Remove(type))
            {
                _logger.LogInformation("Failure cleared: {Type}", type);
            }
        }
    }

    public void ClearAllFailures()
    {
        lock (_lock)
        {
            _failures.Clear();
            _logger.LogInformation("All failures cleared");
        }
    }

    public FailureConfig? GetActiveFailure(FailureType type)
    {
        lock (_lock)
        {
            if (_failures.TryGetValue(type, out var config))
            {
                if (config.DurationMs.HasValue && DateTime.UtcNow - config.InjectedAt > TimeSpan.FromMilliseconds(config.DurationMs.Value))
                {
                    _failures.Remove(type);
                    return null;
                }
                return config;
            }
            return null;
        }
    }

    public IReadOnlyCollection<FailureConfig> GetAllActiveFailures()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var active = _failures.Values
                .Where(f => !f.DurationMs.HasValue || now - f.InjectedAt <= TimeSpan.FromMilliseconds(f.DurationMs.Value))
                .ToList();

            foreach (var expired in _failures.Values.Where(f => f.DurationMs.HasValue && now - f.InjectedAt > TimeSpan.FromMilliseconds(f.DurationMs.Value)))
            {
                _failures.Remove(expired.Type);
            }

            return active;
        }
    }
}