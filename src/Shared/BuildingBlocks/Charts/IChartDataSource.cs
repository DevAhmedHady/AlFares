namespace BuildingBlocks.Charts;

/// <summary>
/// Exposes allow-listed grid data for dashboard chart computation. Business grid modules implement
/// this contract and the dashboard consumes implementations through dependency injection, preserving
/// module isolation.
/// </summary>
public interface IChartDataSource
{
    /// <summary>Gets the stable datasource key.</summary>
    string Key { get; }

    /// <summary>Gets the localized datasource display name.</summary>
    string DisplayName { get; }

    /// <summary>Describes available grouping and aggregation fields.</summary>
    /// <returns>Datasource metadata.</returns>
    ChartDataSourceMetadata Describe();

    /// <summary>Computes a chart series from an allow-listed request.</summary>
    /// <param name="request">Chart computation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Computed chart series.</returns>
    Task<ChartSeries> ComputeAsync(
        ChartComputeRequest request,
        CancellationToken cancellationToken
    );
}
