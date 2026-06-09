using BuildingBlocks.Endpoints;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Catalog.Features.DeleteBook;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Catalog.Endpoints;

public sealed class DeleteBookEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapDelete($"{CatalogRoutes.Books}/{{id:guid}}", async (Guid id, IDispatcher d, CancellationToken ct) =>
                (await d.Send<bool>(new DeleteBookCommand(id), ct))
                    .ToHttpResult(_ => Results.NoContent()))
            .WithTags(CatalogRoutes.BooksTag);
}
