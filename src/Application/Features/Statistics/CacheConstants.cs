namespace skestock.Application.Features.Statistics;

public static class CacheConstants
{
    public const string Statistics = "statistics";

    public static string BuildClassGoodsReceiptCostTag(Guid classId) =>
        $"{Statistics}:class:{classId}:goods-receipt-cost";
}
