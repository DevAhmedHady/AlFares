using BuildingBlocks.Messaging;
using Ordering.Contracts;
using Ordering.Domain;
using SharedKernel;

namespace Ordering.Features.PlaceOrder;

public sealed class PlaceOrderHandler(IOrderRepository repo)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResponse>
{
    public async Task<Result<PlaceOrderResponse>> Handle(PlaceOrderCommand c, CancellationToken ct)
    {
        var orderResult = Order.Place(c.CustomerName, c.Amount);
        if (orderResult.IsFailure)
            return orderResult.Error;

        var order = orderResult.Value;
        repo.Add(order);
        await repo.SaveChangesAsync(ct);

        return new PlaceOrderResponse(order.Id);
    }
}
