namespace skestock.Application.Features.Stock;

public static class CacheConstants
{
    public const string Stock = "stock";
    
    public static string BuildCoarseTag() => $"{Stock}:class:all:location:all";

    /// <summary>
    /// Fine-grained cache tag for stock changes at one class+location pair.
    /// Commands that write <see cref="Domain.Entities.StockBatch"/> rows for a given
    /// (classId, locationId) can invalidate this tag for consumers that use it.
    /// </summary>
    public static string BuildTag(Guid classId, Guid locationId) => $"{Stock}:class:{classId}:location:{locationId}";

    /// <summary>
    /// Cache tag for every current-stock report variant for a class. Commands that write a
    /// <see cref="Domain.Entities.StockBatch"/> for any location under a given class invalidate
    /// this tag so location/category-filtered reports cannot go stale.
    /// </summary>
    public static string BuildClassTag(Guid classId) => $"{Stock}:class:{classId}:location:all";
}
