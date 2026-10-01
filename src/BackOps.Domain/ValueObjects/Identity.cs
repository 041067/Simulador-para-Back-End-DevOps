namespace BackOps.Domain.ValueObjects;

public readonly record struct IdempotencyKey(string Value)
{
    public static IdempotencyKey FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Idempotency key cannot be empty", nameof(value));
        return new IdempotencyKey(value.Trim());
    }

    public static IdempotencyKey New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}

public readonly record struct CorrelationId(string Value)
{
    public static CorrelationId FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Correlation ID cannot be empty", nameof(value));
        return new CorrelationId(value.Trim());
    }

    public static CorrelationId New() => new(Guid.NewGuid().ToString());

    public override string ToString() => Value;
}

public readonly record struct TraceId(string Value)
{
    public static TraceId FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Trace ID cannot be empty", nameof(value));
        return new TraceId(value.Trim());
    }

    public static TraceId New() => new(Guid.NewGuid().ToString("N")[..32]);

    public override string ToString() => Value;
}