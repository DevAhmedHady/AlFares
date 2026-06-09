using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Catalog.Contracts;
using Catalog.Features.CreateBook;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Endpoints;

public sealed class CreateBookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPost(CatalogRoutes.Books, async (CreateBookRequest req, IMapper map, IDispatcher d, CancellationToken ct) =>
                (await d.Send<CreateBookResponse>(map.Map<CreateBookCommand>(req), ct))
                    .ToHttpResult(r => Results.Created($"{CatalogRoutes.Books}/{r.Id}", r)))
            .WithTags(CatalogRoutes.BooksTag);
}
