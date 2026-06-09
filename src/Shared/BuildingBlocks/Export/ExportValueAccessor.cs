using System.Reflection;

namespace BuildingBlocks.Export;

internal static class ExportValueAccessor
{
    public static object? GetValue<T>(T row, string key)
    {
        ArgumentNullException.ThrowIfNull(row);
        var property = typeof(T).GetProperty(key,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property is null
            ? throw new ArgumentException($"Export property '{key}' does not exist on {typeof(T).Name}.", nameof(key))
            : property.GetValue(row);
    }
}
