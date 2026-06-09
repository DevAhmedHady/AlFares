using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Catalog.Contracts;
using Catalog.Features.GetBookById;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Endpoints;

public sealed class GetBookByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet($"{CatalogRoutes.Books}/{{id:guid}}", async (Guid id, IDispatcher d, CancellationToken ct) =>
                (await d.Send<BookResponse>(new GetBookByIdQuery(id), ct))
                    .ToHttpResult())
            .WithTags(CatalogRoutes.BooksTag);
}
