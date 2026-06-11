using BuildingBlocks.Grids;

namespace BuildingBlocks.Export;

/// <summary>Lists supported grid export formats.</summary>
public enum ExportFormat
{
    /// <summary>Microsoft Excel workbook.</summary>
    Xlsx,

    /// <summary>Portable Document Format.</summary>
    Pdf,
}

/// <summary>Defines one exported grid column.</summary>
/// <param name="Key">Response property key.</param>
/// <param name="Header">Localized column heading.</param>
/// <param name="Type">Field value category.</param>
public sealed record ExportColumn(string Key, string Header, GridFieldType Type);

/// <summary>Exports a materialized grid result.</summary>
public interface IGridExporter
{
    /// <summary>Gets the format produced by this exporter.</summary>
    ExportFormat Format { get; }

    /// <summary>Exports rows using the supplied ordered columns.</summary>
    /// <typeparam name="T">Flat response row type.</typeparam>
    /// <param name="rows">Materialized rows.</param>
    /// <param name="columns">Ordered export columns.</param>
    /// <param name="title">Document title.</param>
    /// <returns>Complete file bytes.</returns>
    byte[] Export<T>(IReadOnlyList<T> rows, IReadOnlyList<ExportColumn> columns, string title);
}
