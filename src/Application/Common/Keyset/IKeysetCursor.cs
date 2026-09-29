namespace skestock.Application.Common.Keyset;

/// <summary>
/// Lightweight projection of the sort-key columns of the last row on a page, used to encode the
/// next cursor without materializing the whole entity.
/// </summary>
public interface IKeysetCursor
{
    /// <summary>
    /// Gets the value of an EF sort property (as named in <see cref="IKeysetSortConfiguration{TEntity}.AllowedSortKeys"/>).
    /// </summary>
    object? GetValue(string propertyName);
}
