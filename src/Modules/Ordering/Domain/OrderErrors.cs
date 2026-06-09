using SharedKernel;

namespace Ordering.Domain;

public static class OrderErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Order.NotFound", $"Order with id '{id}' was not found.");

    public static readonly Error CustomerNameEmpty =
        Error.Validation("Order.CustomerName.Empty", "Customer name is required.");

    public static readonly Error AmountNegative =
        Error.Validation("Order.Amount.Negative", "Amount cannot be negative.");
}
