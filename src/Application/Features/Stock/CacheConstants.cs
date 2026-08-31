namespace skestock.Application.Features.Stock;

public static class CacheConstants
{
    public const string Stock = "stock";

    /// <summary>
    /// Fine-grained cache tag for the current-stock report of one class+location pair.
    /// Commands that write <see cref="Domain.Entities.StockBatch"/> rows for a given
    /// (classId, locationId) - currently only GoodsReceipts.CreateGoodsReceipt - must invalidate
    /// this exact tag so the cached report doesn't go stale.
    /// </summary>
    public static string BuildTag(int classId, int locationId) => $"{Stock}:class:{classId}:location:{locationId}";
}
