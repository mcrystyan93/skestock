using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;

// One received stock line. PurchaseKey identifies the purchase event: the goods receipt when the
// line belongs to one, otherwise the transaction itself.
public sealed record PurchaseLine(
    Guid ItemId,
    Guid ClassId,
    Guid PurchaseKey,
    int Quantity,
    decimal UnitPrice,
    DateTimeOffset PurchasedAt);

public static class PurchaseStatisticsCalculator
{
    public static List<ItemPurchaseStatistic> Calculate(
        IReadOnlyCollection<PurchaseLine> lines,
        DateOnly today,
        TimeZoneInfo timeZone,
        DateTimeOffset computedAt)
    {
        var statistics = new List<ItemPurchaseStatistic>();

        foreach (var (scope, days) in new[]
                 {
                     (PurchaseStatisticsScope.Last90Days, 90),
                     (PurchaseStatisticsScope.Last365Days, 365)
                 })
        {
            var windowStartUtc = LocalCalendarDay.GetStartUtc(today.AddDays(-(days - 1)), timeZone);
            statistics.AddRange(lines
                .Where(line => line.PurchasedAt >= windowStartUtc)
                .GroupBy(line => line.ItemId)
                .Select(group => Aggregate(group, scope, null, computedAt)));
        }

        statistics.AddRange(lines
            .GroupBy(line => new { line.ClassId, line.ItemId })
            .Select(group => Aggregate(group, PurchaseStatisticsScope.Class, group.Key.ClassId, computedAt)));

        return statistics;
    }

    private static ItemPurchaseStatistic Aggregate(
        IEnumerable<PurchaseLine> group,
        PurchaseStatisticsScope scope,
        Guid? classId,
        DateTimeOffset computedAt)
    {
        var lines = group.ToList();
        var totalQuantity = lines.Sum(line => line.Quantity);
        var totalValue = lines.Sum(line => line.Quantity * line.UnitPrice);
        var purchaseCount = lines.Select(line => line.PurchaseKey).Distinct().Count();

        return new ItemPurchaseStatistic
        {
            Scope = scope,
            ClassId = classId,
            ItemId = lines[0].ItemId,
            TotalQuantity = totalQuantity,
            TotalValue = Math.Round(totalValue, 2),
            PurchaseCount = purchaseCount,
            AverageQuantity = Math.Round((decimal)totalQuantity / purchaseCount, 2),
            AverageUnitPrice = totalQuantity == 0 ? 0m : Math.Round(totalValue / totalQuantity, 2),
            LastPurchasedAt = lines.Max(line => line.PurchasedAt),
            ComputedAt = computedAt
        };
    }
}
