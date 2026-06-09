using BuildingBlocks.Messaging;
using Ordering.Contracts;

namespace Ordering.Features.PlaceOrder;

public sealed record PlaceOrderCommand(string CustomerName, decimal Amount) : ICommand<PlaceOrderResponse>;
