using System.Globalization;
using System.Text.Json;

namespace skestock.Application.Common.Filtering;

public static class FilterValueParser
{
    public static bool TryParseBoolean(object? value, out bool result)
    {
        switch (value)
        {
            case bool boolValue:
                result = boolValue;
                return true;
            case string stringValue:
                return TryParseBooleanString(stringValue, out result);
            case JsonElement jsonValue:
                return TryParseBooleanJson(jsonValue, out result);
            case sbyte or byte or short or ushort or int or uint or long or ulong:
                return TryParseBooleanNumber(Convert.ToInt64(value, CultureInfo.InvariantCulture), out result);
            default:
                result = default;
                return false;
        }
    }

    private static bool TryParseBooleanJson(JsonElement value, out bool result)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.True:
                result = true;
                return true;
            case JsonValueKind.False:
                result = false;
                return true;
            case JsonValueKind.String:
                return TryParseBooleanString(value.GetString() ?? string.Empty, out result);
            case JsonValueKind.Number when value.TryGetInt64(out var numberValue):
                return TryParseBooleanNumber(numberValue, out result);
            default:
                result = default;
                return false;
        }
    }

    private static bool TryParseBooleanString(string value, out bool result)
    {
        var normalized = value.Trim();

        if (normalized.Length >= 2 &&
            ((normalized[0] == '\'' && normalized[^1] == '\'') ||
             (normalized[0] == '"' && normalized[^1] == '"')))
        {
            normalized = normalized[1..^1].Trim();
        }

        if (bool.TryParse(normalized, out result))
            return true;

        return TryParseBooleanNumber(normalized, out result);
    }

    private static bool TryParseBooleanNumber(string value, out bool result)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numberValue))
            return TryParseBooleanNumber(numberValue, out result);

        result = default;
        return false;
    }

    private static bool TryParseBooleanNumber(long value, out bool result)
    {
        switch (value)
        {
            case 0:
                result = false;
                return true;
            case 1:
                result = true;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
