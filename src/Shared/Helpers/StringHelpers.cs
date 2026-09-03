using System.Text;
using System.Text.Json;

namespace skestock.Shared.Helpers;

public static class StringHelpers
{
    public static bool IsValidJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        // Fast preliminary check
        ReadOnlySpan<char> span = text.AsSpan().Trim();
        if ((!span.StartsWith("{") || !span.EndsWith("}")) &&
            (!span.StartsWith("[") || !span.EndsWith("]")))
        {
            return false;
        }

        // High-performance reader
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(text));
        try
        {
            while (reader.Read()) { }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
