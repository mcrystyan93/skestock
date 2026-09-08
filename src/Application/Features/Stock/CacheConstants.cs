namespace skestock.Application.Features.Stock;

public static class CacheConstants
{
    public const string Stock = "stock";
    
    public static string BuildCoarseTag() => $"{Stock}:class:all:location:all";

    /// <summary>
    /// Fine-grained cache tag for the current-stock report of one class+location pair.
    /// Commands that write <see cref="Domain.Entities.StockBatch"/> rows for a given
    /// (classId, locationId) - currently only GoodsReceipts.CreateGoodsReceipt - must invalidate
    /// this exact tag so the cached report doesn't go stale.
    /// </summary>
    public static string BuildTag(Guid classId, Guid locationId) => $"{Stock}:class:{classId}:location:{locationId}";

    /// <summary>
    /// Cache tag for the current-stock report of a class across every location (LocationId
    /// omitted). Commands that write a <see cref="Domain.Entities.StockBatch"/> for any location
    /// under a given class must also invalidate this tag, since it changes the all-locations report.
    /// </summary>
    public static string BuildClassTag(Guid classId) => $"{Stock}:class:{classId}:location:all";
}
