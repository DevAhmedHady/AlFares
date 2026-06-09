using System.Text.RegularExpressions;
using SharedKernel;

namespace Catalog.Domain;

public sealed partial class Isbn : ValueObject
{
    public string Value { get; }

    private Isbn(string value) => Value = value;

    public static Result<Isbn> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return BookErrors.IsbnEmpty;

        var normalized = value.Replace("-", "").Replace(" ", "").Trim();
        if (!IsbnPattern().IsMatch(normalized))
            return BookErrors.IsbnInvalid;

        return new Isbn(normalized);
    }

    // Trusted reconstruction from persistence — bypasses validation.
    public static Isbn FromPersisted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;

    // ISBN-10 or ISBN-13 (digits, last char of ISBN-10 may be X).
    [GeneratedRegex(@"^(\d{9}[\dX]|\d{13})$")]
    private static partial Regex IsbnPattern();
}
