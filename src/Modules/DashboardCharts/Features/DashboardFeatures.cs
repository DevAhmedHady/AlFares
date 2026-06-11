using System.Text.Json;
using BuildingBlocks.Charts;
using BuildingBlocks.Grids;
using DashboardCharts.Domain;
using DashboardCharts.Services;
using SharedKernel;

namespace DashboardCharts.Features;

/// <summary>Chart request.</summary>
public sealed record ChartRequest(
    string Title,
    ChartType Type,
    string DatasourceKey,
    string XField,
    string? YField,
    ChartAggregation Aggregation,
    string ColorsJson,
    string? FiltersJson,
    int LayoutOrder,
    bool IsEnabled = true
);

/// <summary>Chart response.</summary>
public sealed record ChartResponse(
    Guid Id,
    string Title,
    ChartType Type,
    string DatasourceKey,
    string XField,
    string? YField,
    ChartAggregation Aggregation,
    string ColorsJson,
    string? FiltersJson,
    int LayoutOrder,
    bool IsEnabled
);

/// <summary>Dashboard use cases.</summary>
public sealed class DashboardService(
    IChartDefinitionRepository repo,
    ChartDataSourceRegistry registry
)
{
    /// <summary>Lists definitions.</summary>
    public async Task<Result<IReadOnlyList<ChartResponse>>> ListAsync(CancellationToken ct) =>
        (await repo.ListAsync(ct)).Select(Map).ToArray();

    /// <summary>Creates.</summary>
    public async Task<Result<ChartResponse>> CreateAsync(ChartRequest r, CancellationToken ct)
    {
        if (registry.Get(r.DatasourceKey) is null)
            return ChartErrors.DatasourceNotFound(r.DatasourceKey);
        var d = ChartDefinition.Create(
            r.Title,
            r.Type,
            r.DatasourceKey,
            r.XField,
            r.YField,
            r.Aggregation,
            r.ColorsJson,
            r.FiltersJson,
            r.LayoutOrder
        );
        if (d.IsFailure)
            return d.Error;
        repo.Add(d.Value);
        await repo.SaveAsync(ct);
        return Map(d.Value);
    }

    /// <summary>Updates.</summary>
    public async Task<Result<ChartResponse>> UpdateAsync(
        Guid id,
        ChartRequest r,
        CancellationToken ct
    )
    {
        var d = await repo.GetAsync(id, ct);
        if (d is null)
            return ChartErrors.NotFound(id);
        if (registry.Get(r.DatasourceKey) is null)
            return ChartErrors.DatasourceNotFound(r.DatasourceKey);
        var u = d.Update(
            r.Title,
            r.Type,
            r.DatasourceKey,
            r.XField,
            r.YField,
            r.Aggregation,
            r.ColorsJson,
            r.FiltersJson,
            r.LayoutOrder,
            r.IsEnabled
        );
        if (u.IsFailure)
            return u.Error;
        await repo.SaveAsync(ct);
        return Map(d);
    }

    /// <summary>Deletes.</summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var d = await repo.GetAsync(id, ct);
        if (d is null)
            return Result.Failure(ChartErrors.NotFound(id));
        repo.Remove(d);
        await repo.SaveAsync(ct);
        return Result.Success();
    }

    /// <summary>Gets saved chart data.</summary>
    public async Task<Result<ChartSeries>> DataAsync(Guid id, CancellationToken ct)
    {
        var d = await repo.GetAsync(id, ct);
        if (d is null)
            return ChartErrors.NotFound(id);
        return await Compute(d.DatasourceKey, d.XField, d.YField, d.Aggregation, d.FiltersJson, ct);
    }

    /// <summary>Previews.</summary>
    public Task<Result<ChartSeries>> PreviewAsync(ChartRequest r, CancellationToken ct) =>
        Compute(r.DatasourceKey, r.XField, r.YField, r.Aggregation, r.FiltersJson, ct);

    /// <summary>Lists datasources.</summary>
    public IReadOnlyList<ChartDataSourceMetadata> Datasources() => registry.All();

    private async Task<Result<ChartSeries>> Compute(
        string key,
        string x,
        string? y,
        ChartAggregation a,
        string? json,
        CancellationToken ct
    )
    {
        var source = registry.Get(key);
        if (source is null)
            return ChartErrors.DatasourceNotFound(key);
        var metadata = source.Describe();
        if (!metadata.Fields.Any(field => field.Key == x && field.CanGroupBy))
            return Error.Validation(
                "dashboard.invalid_x_field",
                $"Field '{x}' cannot be used as a chart grouping field."
            );
        if (
            a != ChartAggregation.Count
            && (
                string.IsNullOrWhiteSpace(y)
                || !metadata.Fields.Any(field => field.Key == y && field.CanAggregate)
            )
        )
            return Error.Validation(
                "dashboard.invalid_y_field",
                $"Field '{y}' cannot be aggregated."
            );
        try
        {
            var filters = string.IsNullOrWhiteSpace(json)
                ? Array.Empty<GridFilter>()
                : JsonSerializer.Deserialize<GridFilter[]>(json) ?? [];
            return await source.ComputeAsync(new(x, y, a, filters), ct);
        }
        catch (JsonException exception)
        {
            return Error.Validation("dashboard.invalid_filters", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Error.Validation("dashboard.invalid_chart", exception.Message);
        }
    }

    private static ChartResponse Map(ChartDefinition d) =>
        new(
            d.Id,
            d.Title,
            d.Type,
            d.DatasourceKey,
            d.XField,
            d.YField,
            d.Aggregation,
            d.ColorsJson,
            d.FiltersJson,
            d.LayoutOrder,
            d.IsEnabled
        );
}
