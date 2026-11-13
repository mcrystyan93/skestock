namespace skestock.Application.Features.Statistics;

public static class CacheConstants
{
    public const string Statistics = "statistics";

    public const string DailyConsumptionTag = $"{Statistics}:daily-consumption";

    public const string PurchaseStatisticsTag = $"{Statistics}:purchases";

    public const string DonationStatisticsTag = $"{Statistics}:donations";

    public static string BuildClassGoodsReceiptCostTag(Guid classId) =>
        $"{Statistics}:class:{classId}:goods-receipt-cost";
}
