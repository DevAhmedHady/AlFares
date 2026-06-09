using SharedKernel;

namespace Catalog.Domain;

public sealed class Title : ValueObject
{
    public const int MaxLength = 200;

    public string Value { get; }

    private Title(string value) => Value = value;

    public static Result<Title> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return BookErrors.TitleEmpty;
        if (value.Length > MaxLength)
            return BookErrors.TitleTooLong;
        return new Title(value.Trim());
    }

    // Trusted reconstruction from persistence — bypasses validation.
    public static Title FromPersisted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;
}
