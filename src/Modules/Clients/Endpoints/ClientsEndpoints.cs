using BuildingBlocks.Authentication;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using BuildingBlocks.Http;
using BuildingBlocks.Messaging;
using Clients.Contracts;
using Clients.Domain;
using Clients.Features;
using Clients.Persistence;
using MapsterMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
namespace Clients.Endpoints;

/// <summary>Maps Clients CRUD, grid, and export endpoints.</summary>
public sealed class ClientsEndpoints : IEndpoint
{
    private const string Read = "clients.read";
    private const string Write = "clients.write";
    private const string Delete = "clients.delete";
    private const string Export = "clients.export";

    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ClientsRoutes.Base).WithTags(ClientsRoutes.Tag);
        group.MapPost("", async (CreateClientRequest request, IMapper mapper, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send<ClientResponse>(mapper.Map<CreateClientCommand>(request), ct)).ToHttpResult(value => Results.Created($"{ClientsRoutes.Base}/{value.Id}", value))).RequirePermission(Write);
        group.MapPut("/{id:guid}", async (Guid id, UpdateClientRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send<ClientResponse>(new UpdateClientCommand(id, request.Name, request.ContactName, request.Phone, request.Email, request.AccountBalance, request.ActivityLevel, request.Notes), ct)).ToHttpResult()).RequirePermission(Write);
        group.MapPut("/{id:guid}/status", async (Guid id, SetClientStatusRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send<ClientResponse>(new SetClientStatusCommand(id, request.Status), ct)).ToHttpResult()).RequirePermission(Write);
        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send<bool>(new DeleteClientCommand(id), ct)).ToHttpResult(_ => Results.NoContent())).RequirePermission(Delete);
        group.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send<ClientResponse>(new GetClientByIdQuery(id), ct)).ToHttpResult()).RequirePermission(Read);
        group.MapPost("/grid", async (GridQuery request, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send<PagedResult<ClientResponse>>(new GetClientsGridQuery(request), ct)).ToHttpResult()).RequirePermission(Read);
        group.MapPost("/export", ExportAsync).RequirePermission(Export);
    }

    private static async Task<IResult> ExportAsync(ClientExportRequest request, IMainDbContext db, IGridExporterFactory exporters, CancellationToken ct)
    {
        var applied = db.Set<Client>().AsNoTracking().ApplyGridQuery(request.Grid, ClientGrid.Fields);
        if (applied.IsFailure) return applied.ToHttpResult();
        var rows = await applied.Value.Take(GridExportLimits.MaxRows).Select(ClientGrid.Projection).ToListAsync(ct).ConfigureAwait(false);
        var columns = ClientGrid.Fields.Fields.Select(x => new ExportColumn(ResponseKey(x.Key), x.DisplayName, x.Type)).ToArray();
        var bytes = exporters.For(request.Format).Export(rows, columns, "العملاء");
        var contentType = request.Format == ExportFormat.Xlsx ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "application/pdf";
        var extension = request.Format == ExportFormat.Xlsx ? "xlsx" : "pdf";
        return Results.File(bytes, contentType, $"clients.{extension}");
    }

    private static string ResponseKey(string key) => key switch
    {
        "contactName" => nameof(ClientResponse.ContactName), "accountBalance" => nameof(ClientResponse.AccountBalance),
        "activityLevel" => nameof(ClientResponse.ActivityLevel), "createdAt" => nameof(ClientResponse.CreatedAtUtc),
        _ => char.ToUpperInvariant(key[0]) + key[1..]
    };
}
