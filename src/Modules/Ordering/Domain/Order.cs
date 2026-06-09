using SharedKernel;

namespace Ordering.Domain;

public sealed class Order : AggregateRoot
{
    public string CustomerName { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Order() { } // EF materialization

    private Order(Guid id, string customerName, decimal amount, DateTime createdAtUtc) : base(id)
    {
        CustomerName = customerName;
        Amount = amount;
        CreatedAtUtc = createdAtUtc;
    }

    public static Result<Order> Place(string? customerName, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return OrderErrors.CustomerNameEmpty;
        if (amount < 0)
            return OrderErrors.AmountNegative;

        return new Order(Guid.NewGuid(), customerName.Trim(), decimal.Round(amount, 2), DateTime.UtcNow);
    }
}
