using System.Text.Json;
using skestock.Domain.Common;

namespace skestock.Application.Common.Keyset;

public class CursorCodec<TEntity> where TEntity : class, IKeysetEntity
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string? Encode(
        TEntity lastEntity,
        List<(string Key, string Direction)> effectiveSort,
        IKeysetSortConfiguration<TEntity> sortConfig)
    {
        if (effectiveSort.Count == 0)
            return null;

        var keyValues = new Dictionary<string, object?>();

        // Gather all sort field values from last entity. Null values are stored explicitly
        // (not skipped) so KeysetPredicateBuilder can still build correct tie-breaker predicates
        // for deeper sort keys when a nullable column happens to be null on the boundary row.
        foreach ((string sortKey, var _) in effectiveSort)
        {
            keyValues[sortKey] = sortConfig.GetPropertyValue(lastEntity, sortKey);
        }

        var sort = effectiveSort
            .Select(x => new CursorSortItem { Key = x.Key, Direction = x.Direction })
            .ToList();

        var state = new CursorState(sort, keyValues);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    }

    public static CursorState? Decode(string? cursorToken)
    {
        if (string.IsNullOrWhiteSpace(cursorToken))
            return null;

        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursorToken));
            var state = JsonSerializer.Deserialize<CursorState>(json, JsonOptions);
            if (state is null)
                return null;

            return state with
            {
                Sort = state.Sort ?? [],
                KeyValues = state.KeyValues ?? new Dictionary<string, object?>()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Verifies that the cursor was produced for the exact same sort specification (keys, order
    /// and directions) as the current request's effective sort. A cursor encodes key values that
    /// are only meaningful relative to a specific ORDER BY; reusing it after the sort changes would
    /// silently produce incorrect (duplicated/skipped) pagination results, so callers should reject
    /// mismatched cursors instead of applying them.
    /// </summary>
    public static bool MatchesSort(CursorState state, List<(string Key, string Direction)> effectiveSort)
    {
        if (state.Sort.Count != effectiveSort.Count)
            return false;

        for (var i = 0; i < effectiveSort.Count; i++)
        {
            var expected = effectiveSort[i];
            var actual = state.Sort[i];

            if (!string.Equals(actual.Key, expected.Key, StringComparison.Ordinal) ||
                !string.Equals(actual.Direction, expected.Direction, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
