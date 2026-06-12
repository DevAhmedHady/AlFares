#pragma warning disable CS9113
using BuildingBlocks.Authentication;
using BuildingBlocks.Charts;
using BuildingBlocks.Endpoints;
using BuildingBlocks.Export;
using BuildingBlocks.Grids;
using BuildingBlocks.Http;
using BuildingBlocks.Ledger;
using BuildingBlocks.Modules;
using BuildingBlocks.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revenues.Domain;
using Revenues.Persistence;

namespace Revenues;

/// <summary>Revenue response. Init-only members so the grid's join projection stays translatable for
/// EF Core filtering/sorting (a positional-constructor projection forces client evaluation and throws).</summary>
public sealed record RevenueResponse
{
    public Guid Id { get; init; }
    public Guid RevenueTypeId { get; init; }
    public string RevenueTypeName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public DateOnly Date { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public OwnerType OwnerType { get; init; }
    public Guid? OwnerId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}

/// <summary>Revenue request.</summary>
public sealed record RevenueRequest(
    Guid RevenueTypeId,
    decimal Amount,
    DateOnly Date,
    string Source,
    string? Notes,
    OwnerType OwnerType = OwnerType.General,
    Guid? OwnerId = null
);

/// <summary>Revenue type response.</summary>
public sealed record RevenueTypeResponse(Guid Id, string Name, bool IsActive);

/// <summary>Revenue type request.</summary>
public sealed record RevenueTypeRequest(string Name, bool IsActive = true);

/// <summary>Revenue module.</summary>
public sealed class RevenuesModule : IModule
{
    public string Name => "Revenues";

    public void Register(IServiceCollection s, IConfiguration c)
    {
        s.AddScoped<ILedgerSource, RevenuesLedgerSource>();
        s.AddScoped<IChartDataSource, RevenuesChartDataSource>();
        s.AddHostedService<RevenueSeedService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder e) =>
        e.MapEndpointsFromAssembly(typeof(RevenuesModule).Assembly);
}

/// <summary>Revenue endpoints.</summary>
public sealed class RevenueEndpoints : IEndpoint
{
    private static IQueryable<RevenueResponse> Query(IMainDbContext db) =>
        from x in db.Set<Revenue>().AsNoTracking()
        join t in db.Set<RevenueType>().AsNoTracking() on x.RevenueTypeId equals t.Id
        orderby x.Date descending
        select new RevenueResponse
        {
            Id = x.Id,
            RevenueTypeId = x.RevenueTypeId,
            RevenueTypeName = t.Name,
            Amount = x.Amount,
            Date = x.Date,
            Source = x.Source,
            Notes = x.Notes,
            OwnerType = x.OwnerType,
            OwnerId = x.OwnerId,
            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc,
        };

    private static GridFieldMap<RevenueResponse> Map =>
        new(
            new[]
            {
                (
                    new GridField("revenueTypeId", "النوع", GridFieldType.Text, false),
                    (System.Linq.Expressions.Expression<Func<RevenueResponse, object?>>)(
                        x => x.RevenueTypeId
                    )
                ),
                (
                    new GridField(
                        "revenueTypeName",
                        "النوع",
                        GridFieldType.Text,
                        true,
                        Chartable: true
                    ),
                    (System.Linq.Expressions.Expression<Func<RevenueResponse, object?>>)(
                        x => x.RevenueTypeName
                    )
                ),
                (
                    new GridField("amount", "المبلغ", GridFieldType.Number, false, Chartable: true),
                    (System.Linq.Expressions.Expression<Func<RevenueResponse, object?>>)(
                        x => x.Amount
                    )
                ),
                (
                    new GridField("date", "التاريخ", GridFieldType.Date, false, Chartable: true),
                    (System.Linq.Expressions.Expression<Func<RevenueResponse, object?>>)(
                        x => x.Date
                    )
                ),
                (
                    new GridField("source", "المصدر", GridFieldType.Text, true),
                    (System.Linq.Expressions.Expression<Func<RevenueResponse, object?>>)(
                        x => x.Source
                    )
                ),
                (
                    new GridField("ownerType", "نوع المالك", GridFieldType.Enum, false),
                    (System.Linq.Expressions.Expression<Func<RevenueResponse, object?>>)(
                        x => x.OwnerType
                    )
                ),
                (
                    new GridField("ownerId", "المالك", GridFieldType.Text, false),
                    (System.Linq.Expressions.Expression<Func<RevenueResponse, object?>>)(
                        x => x.OwnerId
                    )
                ),
            }
        );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/revenues").WithTags("Revenues");
        g.MapPost("", Create).RequirePermission("revenues.write");
        g.MapPut("/{id:guid}", Update).RequirePermission("revenues.write");
        g.MapDelete("/{id:guid}", Delete).RequirePermission("revenues.delete");
        g.MapGet("/{id:guid}", Get).RequirePermission("revenues.read");
        g.MapPost("/grid", Grid).RequirePermission("revenues.read");
        g.MapPost("/export", Export).RequirePermission("revenues.export");
        g.MapGet("/types", Types).RequirePermission("revenues.read");
        g.MapPost("/types", CreateType).RequirePermission("revenues.write");
        g.MapPut("/types/{id:guid}", UpdateType).RequirePermission("revenues.write");
        g.MapDelete("/types/{id:guid}", DeleteType).RequirePermission("revenues.delete");
    }

    private static async Task<IResult> Create(
        RevenueRequest r,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var x = Revenue.Create(
            r.RevenueTypeId,
            r.Amount,
            r.Date,
            r.Source,
            r.Notes,
            r.OwnerType,
            r.OwnerId
        );
        if (x.IsFailure)
            return Results.BadRequest(x.Error);
        db.Add(x.Value);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/revenues/{x.Value.Id}",
            await Query(db).SingleAsync(v => v.Id == x.Value.Id, ct)
        );
    }

    private static async Task<IResult> Update(
        Guid id,
        RevenueRequest r,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var x = await db.Set<Revenue>().FindAsync([id], ct);
        if (x is null)
            return Results.NotFound();
        var z = x.Update(
            r.RevenueTypeId,
            r.Amount,
            r.Date,
            r.Source,
            r.Notes,
            r.OwnerType,
            r.OwnerId
        );
        if (z.IsFailure)
            return Results.BadRequest(z.Error);
        await db.SaveChangesAsync(ct);
        return Results.Ok(await Query(db).SingleAsync(v => v.Id == id, ct));
    }

    private static async Task<IResult> Delete(Guid id, IMainDbContext db, CancellationToken ct)
    {
        var x = await db.Set<Revenue>().FindAsync([id], ct);
        if (x is null)
            return Results.NotFound();
        db.Remove(x);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Get(Guid id, IMainDbContext db, CancellationToken ct)
    {
        var x = await Query(db).SingleOrDefaultAsync(v => v.Id == id, ct);
        return x is null ? Results.NotFound() : Results.Ok(x);
    }

    private static async Task<IResult> Grid(GridQuery r, IMainDbContext db, CancellationToken ct)
    {
        var q = Query(db).ApplyGridQuery(r, Map);
        if (q.IsFailure)
            return q.ToHttpResult();
        return Results.Ok(await q.Value.ToPagedResultAsync(r, x => x, ct));
    }

    private static async Task<IResult> Export(
        RevenueExport r,
        IMainDbContext db,
        IGridExporterFactory f,
        CancellationToken ct
    )
    {
        var q = Query(db).ApplyGridQuery(r.Grid, Map);
        if (q.IsFailure)
            return q.ToHttpResult();
        var rows = await q.Value.Take(GridExportLimits.MaxRows).ToListAsync(ct);
        var cols = new[]
        {
            new ExportColumn("RevenueTypeName", "النوع", GridFieldType.Text),
            new ExportColumn("Amount", "المبلغ", GridFieldType.Number),
            new ExportColumn("Date", "التاريخ", GridFieldType.Date),
            new ExportColumn("Source", "المصدر", GridFieldType.Text),
        };
        var bytes = f.For(r.Format).Export(rows, cols, "الإيرادات");
        return Results.File(
            bytes,
            r.Format == ExportFormat.Xlsx
                ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                : "application/pdf",
            $"revenues.{(r.Format == ExportFormat.Xlsx ? "xlsx" : "pdf")}"
        );
    }

    private static async Task<IResult> Types(IMainDbContext db, CancellationToken ct) =>
        Results.Ok(
            await db.Set<RevenueType>()
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new RevenueTypeResponse(x.Id, x.Name, x.IsActive))
                .ToListAsync(ct)
        );

    private static async Task<IResult> CreateType(
        RevenueTypeRequest r,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var x = RevenueType.Create(r.Name);
        if (x.IsFailure)
            return Results.BadRequest(x.Error);
        db.Add(x.Value);
        await db.SaveChangesAsync(ct);
        return Results.Created(
            $"/api/revenues/types/{x.Value.Id}",
            new RevenueTypeResponse(x.Value.Id, x.Value.Name, x.Value.IsActive)
        );
    }

    private static async Task<IResult> UpdateType(
        Guid id,
        RevenueTypeRequest r,
        IMainDbContext db,
        CancellationToken ct
    )
    {
        var x = await db.Set<RevenueType>().FindAsync([id], ct);
        if (x is null)
            return Results.NotFound();
        var z = x.Update(r.Name, r.IsActive);
        if (z.IsFailure)
            return Results.BadRequest(z.Error);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new RevenueTypeResponse(x.Id, x.Name, x.IsActive));
    }

    private static async Task<IResult> DeleteType(Guid id, IMainDbContext db, CancellationToken ct)
    {
        var x = await db.Set<RevenueType>().FindAsync([id], ct);
        if (x is null)
            return Results.NotFound();
        if (await db.Set<Revenue>().AnyAsync(v => v.RevenueTypeId == id, ct))
            return Results.Conflict();
        db.Remove(x);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

/// <summary>Revenue export request.</summary>
public sealed record RevenueExport(GridQuery Grid, ExportFormat Format);

/// <summary>Revenue ledger source.</summary>
public sealed class RevenuesLedgerSource(IMainDbContext db) : ILedgerSource
{
    public LedgerKind Kind => LedgerKind.Revenue;

    public async Task<IReadOnlyList<LedgerEntry>> GetEntriesAsync(
        OwnerType ownerType,
        Guid ownerId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct
    )
    {
        var q =
            from x in db.Set<Revenue>().AsNoTracking()
            join t in db.Set<RevenueType>() on x.RevenueTypeId equals t.Id
            where x.OwnerType == ownerType && x.OwnerId == ownerId
            select new { x, t };
        if (from.HasValue)
            q = q.Where(v => v.x.Date >= from);
        if (to.HasValue)
            q = q.Where(v => v.x.Date <= to);
        return await q.Select(v => new LedgerEntry(
                v.x.Id,
                Kind,
                v.x.OwnerType,
                v.x.OwnerId,
                v.t.Name,
                v.x.Amount,
                v.x.Date
            ))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetTotalsAsync(
        OwnerType ownerType,
        IReadOnlyCollection<Guid> ids,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct
    )
    {
        var q = db.Set<Revenue>()
            .AsNoTracking()
            .Where(x =>
                x.OwnerType == ownerType && x.OwnerId.HasValue && ids.Contains(x.OwnerId.Value)
            );
        if (from.HasValue)
            q = q.Where(x => x.Date >= from);
        if (to.HasValue)
            q = q.Where(x => x.Date <= to);
        return await q.GroupBy(x => x.OwnerId!.Value)
            .ToDictionaryAsync(x => x.Key, x => x.Sum(v => v.Amount), ct);
    }
}

/// <summary>Revenue chart source.</summary>
public sealed class RevenuesChartDataSource(IMainDbContext db) : IChartDataSource
{
    public string Key => "revenues";
    public string DisplayName => "الإيرادات";

    public ChartDataSourceMetadata Describe() =>
        new(
            Key,
            DisplayName,
            [
                new("date", "الشهر", GridFieldType.Date, true, false),
                new("amount", "المبلغ", GridFieldType.Number, false, true),
            ]
        );

    public async Task<ChartSeries> ComputeAsync(ChartComputeRequest r, CancellationToken ct)
    {
        var rows = await db.Set<Revenue>()
            .AsNoTracking()
            .GroupBy(x => new { x.Date.Year, x.Date.Month })
            .Select(g => new ChartPoint(
                $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                r.Aggregation == ChartAggregation.Count ? (decimal)g.Count() : g.Sum(x => x.Amount)
            ))
            .ToListAsync(ct);
        return new(DisplayName, rows);
    }
}

/// <summary>Seeds revenue types.</summary>
public sealed class RevenueSeedService(IServiceProvider services) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IMainDbContext>();
        if (!await db.Set<RevenueType>().AnyAsync(x => x.Name == "مبيعات طوب", ct))
        {
            db.Set<RevenueType>().Add(RevenueType.Create("مبيعات طوب").Value);
            await db.SaveChangesAsync(ct);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
