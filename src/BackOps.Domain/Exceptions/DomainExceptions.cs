namespace BackOps.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class InsufficientInventoryException : DomainException
{
    public int Requested { get; }
    public int Available { get; }

    public InsufficientInventoryException(int requested, int available)
        : base("INSUFFICIENT_INVENTORY", $"Requested {requested} tickets but only {available} available")
    {
        Requested = requested;
        Available = available;
    }
}

public class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message) : base("CONCURRENCY_CONFLICT", message) { }
}

public class IdempotencyKeyExistsException : DomainException
{
    public Guid ExistingEntityId { get; }

    public IdempotencyKeyExistsException(string key, Guid existingEntityId)
        : base("IDEMPOTENCY_KEY_EXISTS", $"Idempotency key '{key}' already used for entity {existingEntityId}")
    {
        ExistingEntityId = existingEntityId;
    }
}

public class CircuitBreakerOpenException : DomainException
{
    public string CircuitName { get; }

    public CircuitBreakerOpenException(string circuitName)
        : base("CIRCUIT_BREAKER_OPEN", $"Circuit breaker '{circuitName}' is open")
    {
        CircuitName = circuitName;
    }
}

public class RateLimitExceededException : DomainException
{
    public int Limit { get; }
    public TimeSpan Window { get; }

    public RateLimitExceededException(int limit, TimeSpan window)
        : base("RATE_LIMIT_EXCEEDED", $"Rate limit exceeded: {limit} requests per {window.TotalSeconds}s")
    {
        Limit = limit;
        Window = window;
    }
}

public class BankUnavailableException : DomainException
{
    public BankStatus BankStatus { get; }

    public BankUnavailableException(BankStatus status)
        : base("BANK_UNAVAILABLE", $"Bank is {status}")
    {
        BankStatus = status;
    }
}

public class QueueFullException : DomainException
{
    public QueueFullException(int capacity)
        : base("QUEUE_FULL", $"Queue is full (capacity: {capacity})")
    {
    }
}