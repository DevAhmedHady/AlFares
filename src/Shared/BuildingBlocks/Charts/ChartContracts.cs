using BuildingBlocks.Grids;

namespace BuildingBlocks.Charts;

/// <summary>Lists supported chart aggregation functions.</summary>
public enum ChartAggregation
{
    /// <summary>Counts rows.</summary>
    Count,

    /// <summary>Sums numeric values.</summary>
    Sum,

    /// <summary>Averages numeric values.</summary>
    Avg,

    /// <summary>Returns minimum value.</summary>
    Min,

    /// <summary>Returns maximum value.</summary>
    Max,
}

/// <summary>Represents one labeled chart value.</summary>
/// <param name="Label">Display label.</param>
/// <param name="Value">Numeric value.</param>
public sealed record ChartPoint(string Label, decimal Value);

/// <summary>Represents one named chart series.</summary>
/// <param name="Name">Series display name.</param>
/// <param name="Points">Ordered chart points.</param>
public sealed record ChartSeries(string Name, IReadOnlyList<ChartPoint> Points);

/// <summary>Describes one field exposed by a chart datasource.</summary>
/// <param name="Key">Stable field key.</param>
/// <param name="DisplayName">Localized display name.</param>
/// <param name="Type">Field value category.</param>
/// <param name="CanGroupBy">Whether the field may be used on the X axis.</param>
/// <param name="CanAggregate">Whether the field may be aggregated on the Y axis.</param>
public sealed record ChartFieldDescriptor(
    string Key,
    string DisplayName,
    GridFieldType Type,
    bool CanGroupBy,
    bool CanAggregate
);

/// <summary>Describes a registered chart datasource.</summary>
/// <param name="Key">Stable datasource key.</param>
/// <param name="DisplayName">Localized display name.</param>
/// <param name="Fields">Allow-listed chart fields.</param>
public sealed record ChartDataSourceMetadata(
    string Key,
    string DisplayName,
    IReadOnlyList<ChartFieldDescriptor> Fields
);

/// <summary>Defines a validated chart computation request.</summary>
/// <param name="XField">Grouping field key.</param>
/// <param name="YField">Optional numeric aggregation field key.</param>
/// <param name="Aggregation">Aggregation function.</param>
/// <param name="Filters">Grid-compatible datasource filters.</param>
public sealed record ChartComputeRequest(
    string XField,
    string? YField,
    ChartAggregation Aggregation,
    IReadOnlyList<GridFilter> Filters
);
