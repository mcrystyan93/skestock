using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;

namespace skestock.Application.Common.Caching;

public static class CacheKeyNormalization
{
    public static string Text(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "_" : value.Trim().ToLowerInvariant();

    public static string Cursor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "_";

        var bytes = Encoding.UTF8.GetBytes(value.Trim());
        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    public static string Bool(bool? value) =>
        value.HasValue ? (value.Value ? "true" : "false") : "_";

    public static string Int(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "_";

    public static string Date(DateTimeOffset? value) =>
        value?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "_";

    public static string Sort(IReadOnlyList<PaginationSort>? sort)
    {
        if (sort is null || sort.Count == 0)
            return "_";

        var builder = new StringBuilder();

        for (var i = 0; i < sort.Count; i++)
        {
            var item = sort[i];

            if (i > 0)
                builder.Append(',');

            builder
                .Append(Uri.EscapeDataString(Text(item.Key)))
                .Append(':')
                .Append(NormalizeSortDirection(item.Value));
        }

        return builder.ToString();
    }

    public static string Filters(IReadOnlyList<ColumnFilter>? filters)
    {
        if (filters is null || filters.Count == 0)
            return "_";

        var builder = new StringBuilder();

        for (var i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];

            if (i > 0)
                builder.Append(',');

            builder
                .Append(Uri.EscapeDataString(Text(filter.Field)))
                .Append(':')
                .Append(Text(filter.Operator.ToString()))
                .Append(':')
                .Append(Uri.EscapeDataString(FilterValue(filter.Value)));
        }

        return builder.ToString();
    }

    private static string NormalizeSortDirection(string? value)
    {
        var normalized = Text(value);

        return normalized switch
        {
            "asc" or "ascend" => "asc",
            "desc" or "descend" => "desc",
            _ => normalized
        };
    }

    private static string FilterValue(object? value)
    {
        if (value is null)
            return "_";

        if (value is JsonElement json)
            return Text(json.ToString());

        if (value is string text)
            return Text(text);

        if (value is bool boolValue)
            return Bool(boolValue);

        if (value is IFormattable formattable)
            return Text(formattable.ToString(null, CultureInfo.InvariantCulture));

        return Text(value.ToString());
    }
}
