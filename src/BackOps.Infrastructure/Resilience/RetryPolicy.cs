namespace BackOps.Infrastructure.Resilience;

using BackOps.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class RetryPolicy : IRetryPolicy
{
    private readonly RetryPolicyOptions _options;
    private readonly ILogger<RetryPolicy> _logger;

    public RetryPolicy(IOptions<RetryPolicyOptions> options, ILogger<RetryPolicy> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= _options.MaxRetries)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (Exception ex) when (IsRetryable(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt > _options.MaxRetries)
                {
                    _logger.LogError(ex, "Retry policy exhausted after {MaxRetries} attempts", _options.MaxRetries);
                    throw;
                }

                var delay = CalculateDelay(attempt);
                _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying in {Delay}ms", attempt, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw lastException ?? new Exception("Retry policy failed");
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async ct =>
        {
            await action(ct);
            return true;
        }, cancellationToken);
    }

    private bool IsRetryable(Exception ex)
    {
        return _options.RetryableExceptions.Count == 0 ||
               _options.RetryableExceptions.Any(t => t.IsInstanceOfType(ex));
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        var delay = TimeSpan.FromMilliseconds(_options.BaseDelayMs * Math.Pow(_options.Multiplier, attempt - 1));
        var jitter = Random.Shared.NextDouble() * _options.JitterMs;
        return delay.Add(TimeSpan.FromMilliseconds(jitter));
    }
}

public class RetryPolicyOptions
{
    public int MaxRetries { get; set; } = 3;
    public int BaseDelayMs { get; set; } = 100;
    public double Multiplier { get; set; } = 2.0;
    public int JitterMs { get; set; } = 100;
    public List<Type> RetryableExceptions { get; set; } = new()
    {
        typeof(HttpRequestException),
        typeof(TimeoutException),
        typeof(TaskCanceledException)
    };
}