namespace BackOps.Domain.Enums;

public enum SimulationScenario
{
    TicketSale = 1,
    VideoStreaming = 2,
    PaymentProcessing = 3,
    ChaosEngineering = 4
}

public enum QueueProvider
{
    Redis = 1,
    Kafka = 2
}

public enum FailureType
{
    DatabaseLatency = 1,
    DatabaseUnavailable = 2,
    RedisUnavailable = 3,
    WorkerFailure = 4,
    NetworkTimeout = 5,
    RandomErrors = 6
}

public enum CircuitBreakerState
{
    Closed = 1,
    Open = 2,
    HalfOpen = 3
}