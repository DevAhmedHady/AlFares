using Mapster;
using Ordering.Contracts;
using Ordering.Domain;
using Ordering.Features.PlaceOrder;

namespace Ordering.Mapping;

public sealed class OrderingMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderResponse>();
        config.NewConfig<PlaceOrderRequest, PlaceOrderCommand>();
    }
}
