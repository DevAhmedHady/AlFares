using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Catalog.Contracts;
using Catalog.Features.GetAllBooks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Endpoints;

public sealed class GetAllBooksEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet(CatalogRoutes.Books, async (IDispatcher d, CancellationToken ct) =>
                (await d.Send<IReadOnlyList<BookResponse>>(new GetAllBooksQuery(), ct))
                    .ToHttpResult())
            .WithTags(CatalogRoutes.BooksTag);
}
