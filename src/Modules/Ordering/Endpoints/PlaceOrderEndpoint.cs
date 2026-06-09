using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordering.Contracts;
using Ordering.Features.PlaceOrder;

namespace Ordering.Endpoints;

public sealed class PlaceOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost(OrderingRoutes.Orders, async (PlaceOrderRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<PlaceOrderResponse>(map.Map<PlaceOrderCommand>(req), ct))
                    .ToHttpResult(r => Results.Created($"{OrderingRoutes.Orders}/{r.Id}", r)))
            .WithTags(OrderingRoutes.OrdersTag);
}
