using System.Linq.Expressions;

namespace skestock.Application.Common.Keyset;

/// <summary>
/// Configuration for dynamic sorting in keyset pagination.
/// Maps API sort keys to EF property expressions and metadata.
/// </summary>
public interface IKeysetSortConfiguration<TEntity> where TEntity : class
{
    /// <summary>
    /// Get the allowed sort key definitions.
    /// Key: API sort key (case-insensitive)
    /// Value: EF property names to sort by (in order)
    /// </summary>
    IReadOnlyDictionary<string, string[]> AllowedSortKeys { get; }

    /// <summary>
    /// Get an expression for the given property name.
    /// Used for dynamic OrderBy/ThenBy building.
    /// </summary>
    Expression<Func<TEntity, dynamic>> GetPropertyExpression(string propertyName);

    /// <summary>
    /// Get the property value from an entity instance for cursor encoding.
    /// </summary>
    object? GetPropertyValue(TEntity entity, string propertyName);

    /// <summary>
    /// Default sort tuple (e.g. descending by Created, then Id).
    /// </summary>
    List<(string Key, string Direction)> DefaultSort { get; }
}
