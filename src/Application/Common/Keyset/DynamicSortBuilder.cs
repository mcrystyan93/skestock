using skestock.Application.Common.Models;

namespace skestock.Application.Common.Keyset;

/// <summary>
/// Builds effective sort tuples from API sort input, with first-wins deduplication.
/// Ensures deterministic sort by always appending Id as tie-breaker.
/// </summary>
public static class DynamicSortBuilder<TEntity> where TEntity : class
{
    public static List<(string Key, string Direction)> BuildEffectiveSort(
        List<PaginationSort> apiSort,
        IKeysetSortConfiguration<TEntity> sortConfig)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var effective = new List<(string, string)>();

        foreach (var item in apiSort)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
                continue;

            var apiKey = item.Key.Trim();
            if (!sortConfig.AllowedSortKeys.TryGetValue(apiKey, out var efProperties))
                continue; // Validator will catch this

            var direction = NormalizeSortDirection(item.Value ?? "");

            // First-wins: add all EF properties from this API key only if not yet added
            foreach (var prop in efProperties)
            {
                if (!seen.Contains(prop))
                {
                    effective.Add((prop, direction));
                    seen.Add(prop);
                }
            }
        }

        // If no valid sort was resolved from the API request, fall back to the config's default sort
        if (effective.Count == 0)
        {
            foreach (var (key, direction) in sortConfig.DefaultSort)
            {
                if (!seen.Contains(key))
                {
                    effective.Add((key, direction));
                    seen.Add(key);
                }
            }
        }

        // Always ensure Id tie-breaker for deterministic pagination
        if (!seen.Contains("Id"))
            effective.Add(("Id", "asc"));

        return effective;
    }

    private static string NormalizeSortDirection(string direction)
    {
        return direction switch
        {
            var d when d.Equals("asc", StringComparison.OrdinalIgnoreCase) ||
                       d.Equals("ascend", StringComparison.OrdinalIgnoreCase) => "asc",
            var d when d.Equals("desc", StringComparison.OrdinalIgnoreCase) ||
                       d.Equals("descend", StringComparison.OrdinalIgnoreCase) => "desc",
            _ => "asc"
        };
    }
}

