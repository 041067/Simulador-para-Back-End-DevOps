namespace BackOps.Infrastructure.Resilience;

using BackOps.Domain.Interfaces;
using BackOps.Domain.Enums;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class CircuitBreaker : ICircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly ILogger<CircuitBreaker> _logger;
    private readonly object _lock = new();
    private CircuitBreakerState _state = CircuitBreakerState.Closed;
    private int _failureCount = 0;
    private int _successCount = 0;
    private DateTime? _lastStateChange;
    private DateTime? _lastFailureTime;

    public string Name { get; }
    public CircuitBreakerState State => _state;

    public event Action<CircuitBreakerState, CircuitBreakerState>? OnStateChanged;

    public CircuitBreaker(string name, IOptions<CircuitBreakerOptions> options, ILogger<CircuitBreaker> logger)
    {
        Name = name;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (_lastStateChange.HasValue && DateTime.UtcNow - _lastStateChange.Value >= _options.Timeout)
            {
                TransitionToHalfOpen();
            }
            else
            {
                throw new CircuitBreakerOpenException(Name);
            }
        }

        try
        {
            var result = await action(cancellationToken);
            OnSuccess();
            return result;
        }
        catch (Exception)
        {
            OnFailure();
            throw;
        }
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        if (_state == CircuitBreakerState.Open)
        {
            if (_lastStateChange.HasValue && DateTime.UtcNow - _lastStateChange.Value >= _options.Timeout)
            {
                TransitionToHalfOpen();
            }
            else
            {
                throw new CircuitBreakerOpenException(Name);
            }
        }

        try
        {
            await action(cancellationToken);
            OnSuccess();
        }
        catch (Exception)
        {
            OnFailure();
            throw;
        }
    }

    private void OnSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                _successCount++;
                if (_successCount >= _options.SuccessThreshold)
                {
                    TransitionToClosed();
                }
            }
        }
    }

    private void OnFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;
            _successCount = 0;

            if (_state == CircuitBreakerState.HalfOpen)
            {
                TransitionToOpen();
            }
            else if (_state == CircuitBreakerState.Closed && _failureCount >= _options.FailureThreshold)
            {
                TransitionToOpen();
            }
        }
    }

    private void TransitionToOpen()
    {
        var oldState = _state;
        _state = CircuitBreakerState.Open;
        _lastStateChange = DateTime.UtcNow;
        _logger.LogWarning("Circuit breaker {Name} opened after {Failures} failures", Name, _failureCount);
        OnStateChanged?.Invoke(oldState, _state);
    }

    private void TransitionToHalfOpen()
    {
        var oldState = _state;
        _state = CircuitBreakerState.HalfOpen;
        _lastStateChange = DateTime.UtcNow;
        _successCount = 0;
        _logger.LogInformation("Circuit breaker {Name} half-opened", Name);
        OnStateChanged?.Invoke(oldState, _state);
    }

    private void TransitionToClosed()
    {
        var oldState = _state;
        _state = CircuitBreakerState.Closed;
        _lastStateChange = DateTime.UtcNow;
        _failureCount = 0;
        _successCount = 0;
        _logger.LogInformation("Circuit breaker {Name} closed", Name);
        OnStateChanged?.Invoke(oldState, _state);
    }

    public void Reset()
    {
        lock (_lock)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0;
            _successCount = 0;
            _lastStateChange = DateTime.UtcNow;
        }
    }
}

public class CircuitBreakerOptions
{
    public int FailureThreshold { get; set; } = 5;
    public int SuccessThreshold { get; set; } = 2;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public int SamplingDuration { get; set; } = 10;
}