namespace Ordering.Contracts;

public sealed record PlaceOrderRequest(string CustomerName, decimal Amount);

public sealed record PlaceOrderResponse(Guid Id);

public sealed record OrderResponse(Guid Id, string CustomerName, decimal Amount, DateTime CreatedAtUtc);
