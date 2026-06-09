using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Ordering.Contracts;
using Ordering.Features.GetOrderById;

namespace Ordering.Endpoints;

public sealed class GetOrderByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet($"{OrderingRoutes.Orders}/{{id:guid}}", async (Guid id, IDispatcher d, CancellationToken ct) =>
                (await d.Send<OrderResponse>(new GetOrderByIdQuery(id), ct))
                    .ToHttpResult())
            .WithTags(OrderingRoutes.OrdersTag);
}
