namespace BackOps.Infrastructure.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using BackOps.Domain.Interfaces;
using StackExchange.Redis;
using Microsoft.EntityFrameworkCore;
using BackOps.Infrastructure.Persistence;
using BackOps.Domain.Enums;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly BackOpsDbContext _context;

    public DatabaseHealthCheck(BackOpsDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(cancellationToken);
            return HealthCheckResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection failed", ex);
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connection;

    public RedisHealthCheck(IConnectionMultiplexer connection)
    {
        _connection = connection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connection.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis connection successful");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis connection failed", ex);
        }
    }
}

public class QueueHealthCheck : IHealthCheck
{
    private readonly IMessageBus _messageBus;

    public QueueHealthCheck(IMessageBus messageBus)
    {
        _messageBus = messageBus;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var depth = await _messageBus.GetQueueDepthAsync("backops:payment", cancellationToken);
            return HealthCheckResult.Healthy($"Queue depth: {depth}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Queue check failed", ex);
        }
    }
}

public class CircuitBreakerHealthCheck : IHealthCheck
{
    private readonly IEnumerable<ICircuitBreaker> _circuitBreakers;

    public CircuitBreakerHealthCheck(IEnumerable<ICircuitBreaker> circuitBreakers)
    {
        _circuitBreakers = circuitBreakers;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var openCircuits = _circuitBreakers.Where(c => c.State == CircuitBreakerState.Open).ToList();

        if (openCircuits.Any())
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Circuit breakers open: {string.Join(", ", openCircuits.Select(c => c.Name))}"));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All circuit breakers closed"));
    }
}
