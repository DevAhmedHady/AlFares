using BuildingBlocks.Charts;
using BuildingBlocks.Grids;
using Clients.Domain;
using Clients.Features;
using Clients.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Clients.Charts;

/// <summary>Exposes allow-listed Clients data for dashboard charts.</summary>
public sealed class ClientsChartDataSource(IMainDbContext dbContext) : IChartDataSource
{
    private readonly IMainDbContext dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    /// <inheritdoc />
    public string Key => "clients";
    /// <inheritdoc />
    public string DisplayName => "العملاء";
    /// <inheritdoc />
    public ChartDataSourceMetadata Describe() => new(Key, DisplayName,
    [
        new("status", "الحالة", GridFieldType.Enum, true, false),
        new("activityLevel", "مستوى النشاط", GridFieldType.Enum, true, false),
        new("createdAt", "تاريخ الإنشاء", GridFieldType.Date, true, false),
        new("accountBalance", "رصيد الحساب", GridFieldType.Number, false, true)
    ]);

    /// <inheritdoc />
    public async Task<ChartSeries> ComputeAsync(ChartComputeRequest request, CancellationToken cancellationToken)
    {
        var filtered = dbContext.Set<Client>().AsNoTracking().ApplyGridQuery(new GridQuery { Filters = request.Filters }, ClientGrid.Fields);
        if (filtered.IsFailure) throw new ArgumentException(filtered.Error.Description, nameof(request));
        var points = request.XField switch
        {
            "status" => await AggregateAsync(filtered.Value, x => x.Status, x => x.ToString(), request, cancellationToken),
            "activityLevel" => await AggregateAsync(filtered.Value, x => x.ActivityLevel, x => x.ToString(), request, cancellationToken),
            "createdAt" => await AggregateByMonthAsync(filtered.Value, request, cancellationToken),
            _ => throw new ArgumentException($"Unknown chart X field '{request.XField}'.", nameof(request))
        };
        return new ChartSeries(DisplayName, points);
    }

    private static async Task<IReadOnlyList<ChartPoint>> AggregateAsync<TKey>(IQueryable<Client> source, System.Linq.Expressions.Expression<Func<Client, TKey>> key, Func<TKey, string> label, ChartComputeRequest request, CancellationToken ct) where TKey : notnull
    {
        if (request.Aggregation != ChartAggregation.Count && request.YField != "accountBalance") throw new ArgumentException("Clients numeric aggregation requires accountBalance.", nameof(request));
        var rows = request.Aggregation switch
        {
            ChartAggregation.Count => await source.GroupBy(key).Select(g => new { Label = g.Key, Value = (decimal)g.Count() }).ToListAsync(ct),
            ChartAggregation.Sum => await source.GroupBy(key).Select(g => new { Label = g.Key, Value = g.Sum(x => x.AccountBalance) }).ToListAsync(ct),
            ChartAggregation.Avg => await source.GroupBy(key).Select(g => new { Label = g.Key, Value = g.Average(x => x.AccountBalance) }).ToListAsync(ct),
            ChartAggregation.Min => await source.GroupBy(key).Select(g => new { Label = g.Key, Value = g.Min(x => x.AccountBalance) }).ToListAsync(ct),
            ChartAggregation.Max => await source.GroupBy(key).Select(g => new { Label = g.Key, Value = g.Max(x => x.AccountBalance) }).ToListAsync(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
        return rows.Select(x => new ChartPoint(label(x.Label), x.Value)).ToArray();
    }

    private static async Task<IReadOnlyList<ChartPoint>> AggregateByMonthAsync(IQueryable<Client> source, ChartComputeRequest request, CancellationToken ct)
    {
        if (request.Aggregation != ChartAggregation.Count && request.YField != "accountBalance") throw new ArgumentException("Clients numeric aggregation requires accountBalance.", nameof(request));
        var rows = request.Aggregation switch
        {
            ChartAggregation.Count => await source.GroupBy(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month }).Select(g => new { g.Key.Year, g.Key.Month, Value = (decimal)g.Count() }).ToListAsync(ct),
            ChartAggregation.Sum => await source.GroupBy(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month }).Select(g => new { g.Key.Year, g.Key.Month, Value = g.Sum(x => x.AccountBalance) }).ToListAsync(ct),
            ChartAggregation.Avg => await source.GroupBy(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month }).Select(g => new { g.Key.Year, g.Key.Month, Value = g.Average(x => x.AccountBalance) }).ToListAsync(ct),
            ChartAggregation.Min => await source.GroupBy(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month }).Select(g => new { g.Key.Year, g.Key.Month, Value = g.Min(x => x.AccountBalance) }).ToListAsync(ct),
            ChartAggregation.Max => await source.GroupBy(x => new { x.CreatedAtUtc.Year, x.CreatedAtUtc.Month }).Select(g => new { g.Key.Year, g.Key.Month, Value = g.Max(x => x.AccountBalance) }).ToListAsync(ct),
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };
        return rows.OrderBy(x => x.Year).ThenBy(x => x.Month).Select(x => new ChartPoint($"{x.Year:D4}-{x.Month:D2}", x.Value)).ToArray();
    }
}
