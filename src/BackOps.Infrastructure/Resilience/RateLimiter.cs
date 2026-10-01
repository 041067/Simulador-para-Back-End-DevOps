namespace BackOps.Infrastructure.Resilience;

using BackOps.Domain.Interfaces;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Logging;

public class RedisRateLimiter : IRateLimiter
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisRateLimiter> _logger;

    public RedisRateLimiter(IConnectionMultiplexer connection, ILogger<RedisRateLimiter> logger)
    {
        _database = connection.GetDatabase();
        _logger = logger;
    }

    public async Task<bool> TryAcquireAsync(string key, int permits = 1, CancellationToken cancellationToken = default)
    {
        var redisKey = $"ratelimit:{key}";
        var current = await _database.StringGetAsync(redisKey);
        var currentCount = current.HasValue ? (int)current : 0;

        if (currentCount + permits > GetLimit(key))
        {
            return false;
        }

        var newCount = await _database.StringIncrementAsync(redisKey, permits);
        if (newCount == permits)
        {
            await _database.KeyExpireAsync(redisKey, GetWindow(key), CommandFlags.FireAndForget);
        }

        return true;
    }

    public async Task ReleaseAsync(string key, int permits = 1, CancellationToken cancellationToken = default)
    {
        var redisKey = $"ratelimit:{key}";
        await _database.StringDecrementAsync(redisKey, permits);
    }

    public RateLimitInfo GetInfo(string key)
    {
        var redisKey = $"ratelimit:{key}";
        var current = _database.StringGet(redisKey);
        var currentCount = current.HasValue ? (int)current : 0;
        var limit = GetLimit(key);
        var remaining = Math.Max(0, limit - currentCount);

        return new RateLimitInfo(limit, remaining, GetWindow(key), currentCount >= limit);
    }

    private int GetLimit(string key) => 100;
    private TimeSpan GetWindow(string key) => TimeSpan.FromMinutes(1);
}