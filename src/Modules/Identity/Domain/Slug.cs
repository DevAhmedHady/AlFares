using System.Text.RegularExpressions;
using SharedKernel;

namespace Identity.Domain;

public sealed partial class Slug : ValueObject
{
    public const int MaxLength = 60;

    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Result<Slug> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return IdentityErrors.SlugEmpty;

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength || !SlugPattern().IsMatch(normalized))
            return IdentityErrors.SlugInvalid;

        return new Slug(normalized);
    }

    public static Slug FromPersisted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    // lowercase alphanumerics separated by single hyphens, e.g. "acme-corp".
    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
