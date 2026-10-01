namespace BackOps.Infrastructure.Messaging;

using StackExchange.Redis;
using System.Text.Json;
using BackOps.Domain.Interfaces;
using BackOps.Domain.ValueObjects;
using BackOps.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class RedisMessageBus : IMessageBus, IQueueProvider
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly ILogger<RedisMessageBus> _logger;
    private readonly RedisOptions _options;

    public QueueProvider ProviderType => QueueProvider.Redis;

    public RedisMessageBus(
        IConnectionMultiplexer connection,
        ILogger<RedisMessageBus> logger,
        IOptions<RedisOptions> options)
    {
        _connection = connection;
        _database = connection.GetDatabase();
        _logger = logger;
        _options = options.Value;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var queueName = GetQueueName<T>();
        var json = JsonSerializer.Serialize(message);
        var entry = new QueueEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Payload = json,
            Type = typeof(T).Name,
            CreatedAt = DateTime.UtcNow
        };

        var entryJson = JsonSerializer.Serialize(entry);
        await _database.ListRightPushAsync(queueName, entryJson);

        _logger.LogDebug("Published message to queue {QueueName}: {Type}", queueName, typeof(T).Name);
    }

    public async Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class
    {
        var queueName = GetQueueName<T>();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await _database.ListLeftPopAsync(queueName);
                if (result.IsNullOrEmpty)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                var entry = JsonSerializer.Deserialize<QueueEntry>(result!);
                if (entry == null) continue;

                var message = JsonSerializer.Deserialize<T>(entry.Payload);
                if (message == null) continue;

                await handler(message, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public async Task<long> GetQueueDepthAsync(string queueName, CancellationToken cancellationToken = default)
    {
        return await _database.ListLengthAsync(queueName);
    }

    public async Task<int> GetPendingCountAsync(string queueName, CancellationToken cancellationToken = default)
    {
        var length = await _database.ListLengthAsync(queueName);
        return (int)length;
    }

    public async Task EnqueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(message);
        var entry = new QueueEntry
        {
            Id = Guid.NewGuid().ToString("N"),
            Payload = json,
            Type = typeof(T).Name,
            CreatedAt = DateTime.UtcNow
        };

        var entryJson = JsonSerializer.Serialize(entry);
        await _database.ListRightPushAsync(queueName, entryJson);
    }

    public async Task<T?> DequeueAsync<T>(string queueName, CancellationToken cancellationToken = default) where T : class
    {
        var result = await _database.ListLeftPopAsync(queueName);
        if (result.IsNullOrEmpty) return null;

        var entry = JsonSerializer.Deserialize<QueueEntry>(result!);
        if (entry == null) return null;

        return JsonSerializer.Deserialize<T>(entry.Payload);
    }

    public async Task<long> GetDepthAsync(string queueName, CancellationToken cancellationToken = default)
    {
        return await _database.ListLengthAsync(queueName);
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private string GetQueueName<T>() => $"backops:{typeof(T).Name.ToLowerInvariant()}";

    private class QueueEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

public class RedisOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public int MaxQueueSize { get; set; } = 100000;
}