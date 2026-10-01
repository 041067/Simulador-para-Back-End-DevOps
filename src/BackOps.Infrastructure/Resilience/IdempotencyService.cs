namespace BackOps.Infrastructure.Resilience;

using BackOps.Domain.Interfaces;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Exceptions;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class RedisIdempotencyService : IIdempotencyService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisIdempotencyService> _logger;
    private readonly TimeSpan _ttl = TimeSpan.FromHours(24);

    public RedisIdempotencyService(IConnectionMultiplexer connection, ILogger<RedisIdempotencyService> logger)
    {
        _database = connection.GetDatabase();
        _logger = logger;
    }

    public async Task<IdempotencyResult<T>> ExecuteAsync<T>(IdempotencyKey key, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        var redisKey = $"idempotency:{key.Value}";

        var existing = await _database.StringGetAsync(redisKey);
        if (existing.HasValue)
        {
            var existingId = Guid.Parse(existing!);
            _logger.LogInformation("Idempotency key {Key} already exists for entity {EntityId}", key.Value, existingId);
            return new IdempotencyResult<T>(false, default, existingId);
        }

        var result = await action(cancellationToken);

        if (result is IEntityWithId entity)
        {
            await _database.StringSetAsync(redisKey, entity.Id.ToString(), _ttl);
        }
        else if (result is Guid guidResult)
        {
            await _database.StringSetAsync(redisKey, guidResult.ToString(), _ttl);
        }

        return new IdempotencyResult<T>(true, result, null);
    }

    public async Task<IdempotencyResult> ExecuteAsync(IdempotencyKey key, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(key, async ct =>
        {
            await action(ct);
            return Guid.NewGuid();
        }, cancellationToken);

        return new IdempotencyResult(result.IsNew, result.ExistingEntityId);
    }

    public async Task<bool> ExistsAsync(IdempotencyKey key, CancellationToken cancellationToken = default)
    {
        var redisKey = $"idempotency:{key.Value}";
        return await _database.KeyExistsAsync(redisKey);
    }
}

public interface IEntityWithId
{
    Guid Id { get; }
}