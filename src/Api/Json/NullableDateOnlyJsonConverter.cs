using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Json;

/// <summary>
/// Reads <see cref="DateOnly"/>? leniently: <c>null</c>, an empty string, or whitespace all map to
/// <c>null</c> rather than throwing. The built-in converter throws on an empty string, which surfaces
/// as an unhandled 500 when clients (e.g. an empty date filter) post <c>""</c>. Accepts the ISO
/// <c>yyyy-MM-dd</c> form and tolerates a full ISO timestamp by taking its date part.
/// </summary>
public sealed class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string DateFormat = "yyyy-MM-dd";

    /// <inheritdoc />
    public override DateOnly? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (
            DateOnly.TryParseExact(
                text,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exact
            )
        )
            return exact;

        // Tolerate a full ISO timestamp ("2026-05-01T00:00:00…") by keeping the date part.
        if (
            DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dateTime
            )
        )
            return DateOnly.FromDateTime(dateTime);

        throw new JsonException($"Could not parse '{text}' as a date (expected {DateFormat}).");
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        DateOnly? value,
        JsonSerializerOptions options
    )
    {
        if (value is null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
    }
}
