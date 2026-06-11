using BuildingBlocks.Charts;
using BuildingBlocks.Grids;
using Expenses.Features;
using Microsoft.EntityFrameworkCore;

namespace Expenses.Charts;

/// <summary>Provides expense chart data.</summary>
public sealed class ExpensesChartDataSource(IMainDbContext db) : IChartDataSource
{
    /// <inheritdoc />
    public string Key => "expenses";

    /// <inheritdoc />
    public string DisplayName => "المصروفات";

    /// <inheritdoc />
    public ChartDataSourceMetadata Describe() =>
        new(
            Key,
            DisplayName,
            [
                new("expenseTypeName", "النوع", GridFieldType.Text, true, false),
                new("date", "الشهر", GridFieldType.Date, true, false),
                new("amount", "المبلغ", GridFieldType.Number, false, true),
            ]
        );

    /// <inheritdoc />
    public async Task<ChartSeries> ComputeAsync(
        ChartComputeRequest request,
        CancellationToken cancellationToken
    )
    {
        var applied = ExpenseGrid
            .Query(db)
            .ApplyGridQuery(new GridQuery { Filters = request.Filters }, ExpenseGrid.Fields);
        if (applied.IsFailure)
            throw new ArgumentException(applied.Error.Description, nameof(request));

        IReadOnlyList<ChartPoint> points;
        if (request.XField == "expenseTypeName")
        {
            var rows = await applied
                .Value.Select(row => new { Label = row.ExpenseTypeName, row.Amount })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            points = rows.GroupBy(row => row.Label)
                .Select(group => new ChartPoint(
                    group.Key,
                    Aggregate(group.Select(row => row.Amount), request.Aggregation)
                ))
                .OrderBy(point => point.Label)
                .ToArray();
        }
        else if (request.XField == "date")
        {
            var rows = await applied
                .Value.Select(row => new { row.Date, row.Amount })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            points = rows.GroupBy(row => new { row.Date.Year, row.Date.Month })
                .OrderBy(group => group.Key.Year)
                .ThenBy(group => group.Key.Month)
                .Select(group => new ChartPoint(
                    $"{group.Key.Year:D4}-{group.Key.Month:D2}",
                    Aggregate(group.Select(row => row.Amount), request.Aggregation)
                ))
                .ToArray();
        }
        else
        {
            throw new ArgumentException("Unknown expense chart grouping field.", nameof(request));
        }

        return new ChartSeries(DisplayName, points);
    }

    private static decimal Aggregate(IEnumerable<decimal> values, ChartAggregation aggregation)
    {
        var amounts = values as decimal[] ?? values.ToArray();
        return aggregation switch
        {
            ChartAggregation.Count => amounts.Length,
            ChartAggregation.Sum => amounts.Sum(),
            ChartAggregation.Avg => amounts.Average(),
            ChartAggregation.Min => amounts.Min(),
            ChartAggregation.Max => amounts.Max(),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregation), aggregation, null),
        };
    }
}
