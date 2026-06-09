using SharedKernel;

namespace Catalog.Domain;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Result<Money> Create(decimal amount)
    {
        if (amount < 0)
            return BookErrors.PriceNegative;
        return new Money(decimal.Round(amount, 2));
    }

    // Trusted reconstruction from persistence — bypasses validation.
    public static Money FromPersisted(decimal amount) => new(amount);

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Amount; }

    public override string ToString() => Amount.ToString("0.00");
}
