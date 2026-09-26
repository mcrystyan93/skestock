namespace skestock.Application.Features.Statistics;

public static class CacheConstants
{
    public const string Statistics = "statistics";

    public const string DailyConsumptionTag = $"{Statistics}:daily-consumption";

    public static string BuildClassGoodsReceiptCostTag(Guid classId) =>
        $"{Statistics}:class:{classId}:goods-receipt-cost";
}
