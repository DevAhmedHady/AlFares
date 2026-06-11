namespace BuildingBlocks.Grids;

/// <summary>Defines a safe server-side grid request.</summary>
public sealed record GridQuery
{
    /// <summary>Gets the one-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets the requested page size.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>Gets the optional global search term.</summary>
    public string? Search { get; init; }

    /// <summary>Gets ordered sort expressions.</summary>
    public IReadOnlyList<GridSort> Sort { get; init; } = [];

    /// <summary>Gets column filters combined with logical AND.</summary>
    public IReadOnlyList<GridFilter> Filters { get; init; } = [];
}

/// <summary>Defines one grid sort expression.</summary>
/// <param name="Field">Allow-listed field key.</param>
/// <param name="Desc">Whether sorting is descending.</param>
public sealed record GridSort(string Field, bool Desc = false);

/// <summary>Lists supported grid filter operations.</summary>
public enum GridFilterOp
{
    /// <summary>Equals.</summary>
    Eq,

    /// <summary>Does not equal.</summary>
    Neq,

    /// <summary>Contains text.</summary>
    Contains,

    /// <summary>Starts with text.</summary>
    StartsWith,

    /// <summary>Greater than.</summary>
    Gt,

    /// <summary>Greater than or equal.</summary>
    Gte,

    /// <summary>Less than.</summary>
    Lt,

    /// <summary>Less than or equal.</summary>
    Lte,

    /// <summary>Between two inclusive values.</summary>
    Between,

    /// <summary>Contained in a comma-separated value set.</summary>
    In,
}

/// <summary>Defines one grid filter.</summary>
/// <param name="Field">Allow-listed field key.</param>
/// <param name="Op">Filter operation.</param>
/// <param name="Value">Primary serialized value.</param>
/// <param name="Value2">Optional second serialized value.</param>
public sealed record GridFilter(
    string Field,
    GridFilterOp Op,
    string? Value,
    string? Value2 = null
);
