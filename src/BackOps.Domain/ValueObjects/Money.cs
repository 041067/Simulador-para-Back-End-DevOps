namespace BackOps.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency = "BRL")
{
    public static Money Zero(string currency = "BRL") => new(0, currency);

    public static Money FromDecimal(decimal amount, string currency = "BRL") => new(amount, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal multiplier) => new(Amount * multiplier, Currency);

    public override string ToString() => $"{Amount:N2} {Currency}";
}