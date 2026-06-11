using BuildingBlocks.Charts;

namespace DashboardCharts.Services;

/// <summary>Registry over DI-provided chart datasources.</summary>
public sealed class ChartDataSourceRegistry
{
    private readonly IReadOnlyDictionary<string, IChartDataSource> sources;

    /// <summary>Creates registry.</summary>
    public ChartDataSourceRegistry(IEnumerable<IChartDataSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Lists metadata.</summary>
    public IReadOnlyList<ChartDataSourceMetadata> All() =>
        sources.Values.Select(x => x.Describe()).OrderBy(x => x.DisplayName).ToArray();

    /// <summary>Gets datasource or null.</summary>
    public IChartDataSource? Get(string key) => sources.GetValueOrDefault(key);
}
