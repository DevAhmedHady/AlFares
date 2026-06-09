using System.Linq.Expressions;

namespace BuildingBlocks.Grids;

/// <summary>Describes supported field value categories.</summary>
public enum GridFieldType
{
    /// <summary>Text value.</summary>
    Text,
    /// <summary>Numeric value.</summary>
    Number,
    /// <summary>Date or date-time value.</summary>
    Date,
    /// <summary>Boolean value.</summary>
    Boolean,
    /// <summary>Enumeration value.</summary>
    Enum
}

/// <summary>Describes one allow-listed grid field.</summary>
/// <param name="Key">Stable API field key.</param>
/// <param name="DisplayName">Localized display name.</param>
/// <param name="Type">Field value category.</param>
/// <param name="Searchable">Whether global search includes the field.</param>
/// <param name="Sortable">Whether clients may sort by the field.</param>
/// <param name="Filterable">Whether clients may filter by the field.</param>
/// <param name="Chartable">Whether chart metadata may expose the field.</param>
public sealed record GridField(string Key, string DisplayName, GridFieldType Type, bool Searchable,
    bool Sortable = true, bool Filterable = true, bool Chartable = false);

/// <summary>Maps public grid keys to safe entity selectors.</summary>
/// <typeparam name="T">Entity type queried by the grid.</typeparam>
public sealed class GridFieldMap<T>
{
    private readonly IReadOnlyDictionary<string, Expression<Func<T, object?>>> selectors;
    private readonly IReadOnlyDictionary<string, GridField> fieldsByKey;

    /// <summary>Initializes a field map from metadata-selector pairs.</summary>
    /// <param name="entries">Allow-listed fields and selectors.</param>
    public GridFieldMap(IEnumerable<(GridField Field, Expression<Func<T, object?>> Selector)> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var materialized = entries.ToArray();
        selectors = materialized.ToDictionary(x => x.Field.Key, x => x.Selector, StringComparer.OrdinalIgnoreCase);
        fieldsByKey = materialized.ToDictionary(x => x.Field.Key, x => x.Field, StringComparer.OrdinalIgnoreCase);
        Fields = materialized.Select(x => x.Field).ToArray();
    }

    /// <summary>Gets ordered field metadata.</summary>
    public IReadOnlyList<GridField> Fields { get; }

    /// <summary>Gets the safe selector for a field key.</summary>
    /// <param name="key">Public field key.</param>
    /// <returns>Registered selector.</returns>
    public Expression<Func<T, object?>> Selector(string key) => selectors.TryGetValue(key, out var selector)
        ? selector : throw new KeyNotFoundException($"Grid field '{key}' is not registered.");

    /// <summary>Attempts to get metadata and selector for a field key.</summary>
    /// <param name="key">Public field key.</param>
    /// <param name="field">Matched metadata.</param>
    /// <param name="selector">Matched selector.</param>
    /// <returns>Whether the key is registered.</returns>
    public bool TryGet(string key, out GridField? field, out Expression<Func<T, object?>>? selector)
    {
        var foundField = fieldsByKey.TryGetValue(key, out field);
        var foundSelector = selectors.TryGetValue(key, out selector);
        return foundField && foundSelector;
    }
}
