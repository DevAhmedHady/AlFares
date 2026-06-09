using BuildingBlocks.Authentication;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using BuildingBlocks.Ledger;
using BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Reports;

/// <summary>Owner ledger request.</summary>
public sealed record OwnerLedgerRequest(OwnerType OwnerType, Guid OwnerId, DateOnly? From, DateOnly? To);

/// <summary>Owner export request.</summary>
public sealed record OwnerLedgerExportRequest(
    OwnerType OwnerType,
    Guid OwnerId,
    DateOnly? From,
    DateOnly? To,
    ExportFormat Format,
    string OwnerName);

/// <summary>Owner balances request.</summary>
public sealed record OwnerBalancesRequest(OwnerType OwnerType, IReadOnlyCollection<Guid> Ids);

/// <summary>Owner balance.</summary>
public sealed record OwnerBalance(decimal Expenses, decimal Revenues, decimal Net);

/// <summary>Owner ledger result.</summary>
public sealed record OwnerLedgerResponse(
    decimal TotalExpenses,
    decimal TotalRevenues,
    decimal Net,
    IReadOnlyList<LedgerEntry> Entries);

/// <summary>Read-only reports module.</summary>
public sealed class ReportsModule : IModule
{
    /// <inheritdoc />
    public string Name => "Reports";

    /// <inheritdoc />
    public void Register(IServiceCollection services, IConfiguration config)
    {
    }

    /// <inheritdoc />
    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapEndpointsFromAssembly(typeof(ReportsModule).Assembly);
}

/// <summary>Owner report endpoints.</summary>
public sealed class ReportsEndpoints : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").WithTags("Reports");
        group.MapPost("/owner-ledger", Ledger).RequirePermission("reports.read");
        group.MapPost("/owner-balances", Balances).RequirePermission("reports.read");
        group.MapPost("/owner-ledger/export", Export).RequirePermission("reports.read");
    }

    private static async Task<OwnerLedgerResponse> Build(
        OwnerLedgerRequest request,
        IEnumerable<ILedgerSource> sources,
        CancellationToken cancellationToken)
    {
        var entries = new List<LedgerEntry>();

        // Ledger sources share the scoped MainDbContext, so EF operations cannot overlap.
        foreach (var source in sources)
        {
            entries.AddRange(await source.GetEntriesAsync(
                request.OwnerType,
                request.OwnerId,
                request.From,
                request.To,
                cancellationToken));
        }

        var rows = entries
            .OrderBy(entry => entry.Date)
            .ThenBy(entry => entry.Kind)
            .ToArray();
        var expenses = rows.Where(entry => entry.Kind == LedgerKind.Expense).Sum(entry => entry.Amount);
        var revenues = rows.Where(entry => entry.Kind == LedgerKind.Revenue).Sum(entry => entry.Amount);

        return new(expenses, revenues, revenues - expenses, rows);
    }

    private static async Task<IResult> Ledger(
        OwnerLedgerRequest request,
        IEnumerable<ILedgerSource> sources,
        CancellationToken cancellationToken) =>
        Results.Ok(await Build(request, sources, cancellationToken));

    private static async Task<IResult> Balances(
        OwnerBalancesRequest request,
        IEnumerable<ILedgerSource> sources,
        CancellationToken cancellationToken)
    {
        var result = request.Ids.ToDictionary(id => id, _ => new OwnerBalance(0, 0, 0));
        foreach (var source in sources)
        {
            var totals = await source.GetTotalsAsync(
                request.OwnerType,
                request.Ids,
                null,
                null,
                cancellationToken);
            foreach (var (id, total) in totals)
            {
                var old = result[id];
                result[id] = source.Kind == LedgerKind.Expense
                    ? new(total, old.Revenues, old.Revenues - total)
                    : new(old.Expenses, total, total - old.Expenses);
            }
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> Export(
        OwnerLedgerExportRequest request,
        IEnumerable<ILedgerSource> sources,
        IGridExporterFactory exporterFactory,
        CancellationToken cancellationToken)
    {
        var ledger = await Build(
            new(request.OwnerType, request.OwnerId, request.From, request.To),
            sources,
            cancellationToken);
        var columns = new[]
        {
            new ExportColumn("Date", "التاريخ", GridFieldType.Date),
            new ExportColumn("Kind", "النوع", GridFieldType.Enum),
            new ExportColumn("Description", "البيان", GridFieldType.Text),
            new ExportColumn("Amount", "المبلغ", GridFieldType.Number)
        };
        var title = $"تقرير {request.OwnerName} | مصروفات {ledger.TotalExpenses:N2} | "
            + $"إيرادات {ledger.TotalRevenues:N2} | صافي {ledger.Net:N2}";
        var bytes = exporterFactory.For(request.Format).Export(ledger.Entries, columns, title);
        var contentType = request.Format == ExportFormat.Xlsx
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "application/pdf";
        var extension = request.Format == ExportFormat.Xlsx ? "xlsx" : "pdf";

        return Results.File(bytes, contentType, $"owner-ledger.{extension}");
    }
}
