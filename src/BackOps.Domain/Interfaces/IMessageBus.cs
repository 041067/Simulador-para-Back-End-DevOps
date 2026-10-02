namespace BackOps.Domain.Interfaces;

using BackOps.Domain.Enums;

using BackOps.Domain.ValueObjects;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class;
    Task<long> GetQueueDepthAsync(string queueName, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(string queueName, CancellationToken cancellationToken = default);
}

public interface IQueueProvider
{
    QueueProvider ProviderType { get; }
    Task EnqueueAsync<T>(string queueName, T message, CancellationToken cancellationToken = default) where T : class;
    Task<T?> DequeueAsync<T>(string queueName, CancellationToken cancellationToken = default) where T : class;
    Task<long> GetDepthAsync(string queueName, CancellationToken cancellationToken = default);
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}