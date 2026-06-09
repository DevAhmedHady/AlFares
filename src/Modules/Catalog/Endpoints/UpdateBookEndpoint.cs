using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Catalog.Contracts;
using Catalog.Features.UpdateBook;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Endpoints;

public sealed class UpdateBookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut($"{CatalogRoutes.Books}/{{id:guid}}", async (Guid id, UpdateBookRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<bool>(map.Map<UpdateBookCommand>(req) with { Id = id }, ct))
                    .ToHttpResult(_ => Results.NoContent()))
            .WithTags(CatalogRoutes.BooksTag);
}
