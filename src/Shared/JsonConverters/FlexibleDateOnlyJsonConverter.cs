using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace skestock.Shared.JsonConverters;


public sealed class FlexibleDateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private static readonly string[] Formats =
    {
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy",
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss"
    };

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
            throw new JsonException("Expected a non-empty date string.");

        if (DateOnly.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateOnly))
            return dateOnly;

        if (DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            return DateOnly.FromDateTime(dateTime);

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
            return DateOnly.FromDateTime(fallback);

        throw new JsonException($"Unable to parse '{value}' as a date.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
