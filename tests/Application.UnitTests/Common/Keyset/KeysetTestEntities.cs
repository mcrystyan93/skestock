using System.Linq.Expressions;
using skestock.Application.Common.Keyset;
using skestock.Domain.Common;

namespace skestock.Application.UnitTests.Common.Keyset;

/// <summary>
/// Minimal standalone entity/config pair used to exercise the generic keyset building blocks
/// (<see cref="KeysetPredicateBuilder{TEntity}"/>, <see cref="OrderByBuilder{TEntity}"/>,
/// <see cref="CursorCodec{TEntity}"/>) in isolation, without depending on EF Core or a real feature.
/// </summary>
public sealed class KeysetTestItem : IKeysetEntity
{
    public int Id { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public int? Priority { get; init; }
}

/// <summary>
/// The logical sort key "created" deliberately maps to a CLR property named "Created" rather than
/// "CreatedDate" - this mirrors <c>CategorySortConfiguration</c> (which maps "createdDate" to the
/// EF property name "Created" while the real property is "CreatedDate") and is exactly the
/// mismatch that used to make <c>KeysetPredicateBuilder</c>'s reflection-by-name property lookup
/// silently fail to build any predicate for the default sort.
/// </summary>
public sealed class KeysetTestItemSortConfiguration : IKeysetSortConfiguration<KeysetTestItem>
{
    public IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; } =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["createdDate"] = ["CreatedDate", "Id"],
            ["priority"] = ["Priority", "Id"]
        };

    public List<(string Key, string Direction)> DefaultSort { get; } = [("CreatedDate", "desc"), ("Id", "desc")];

    public Expression<Func<KeysetTestItem, dynamic>> GetPropertyExpression(string propertyName) => propertyName switch
    {
        "CreatedDate" => e => e.CreatedDate,
        "Priority" => e => e.Priority!,
        "Id" => e => e.Id,
        _ => e => e.Id
    };

    public object? GetPropertyValue(KeysetTestItem entity, string propertyName) => propertyName switch
    {
        "CreatedDate" => entity.CreatedDate,
        "Priority" => entity.Priority,
        "Id" => entity.Id,
        _ => null
    };
}
