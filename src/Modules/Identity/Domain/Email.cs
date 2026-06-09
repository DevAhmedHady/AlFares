using System.Text.RegularExpressions;
using SharedKernel;

namespace Identity.Domain;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return IdentityErrors.EmailEmpty;

        var normalized = value.Trim().ToLowerInvariant();
        if (!EmailPattern().IsMatch(normalized))
            return IdentityErrors.EmailInvalid;

        return new Email(normalized);
    }

    // Trusted reconstruction from persistence — bypasses validation.
    public static Email FromPersisted(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
