using skestock.Domain.Entities;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;

// One goods receipt line. PurchaseKey identifies the purchase event (the goods receipt).
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
        DateTimeOffset computedAt,
        IReadOnlyCollection<ClassPeriod> classPeriods)
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
                .Select(group => Aggregate(group, scope, null, computedAt, timeZone, classPeriods)));
        }

        statistics.AddRange(lines
            .GroupBy(line => new { line.ClassId, line.ItemId })
            .Select(group => Aggregate(group, PurchaseStatisticsScope.Class, group.Key.ClassId, computedAt, timeZone, classPeriods)));

        return statistics;
    }

    private static ItemPurchaseStatistic Aggregate(
        IEnumerable<PurchaseLine> group,
        PurchaseStatisticsScope scope,
        Guid? classId,
        DateTimeOffset computedAt,
        TimeZoneInfo timeZone,
        IReadOnlyCollection<ClassPeriod> classPeriods)
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
            AverageIntervalDays = scope == PurchaseStatisticsScope.Last365Days
                ? AverageIntervalDays(lines, purchaseCount, timeZone, classPeriods)
                : null,
            LastQuantity = LastReceiptQuantity(lines),
            LastPurchasedAt = lines.Max(line => line.PurchasedAt),
            ComputedAt = computedAt
        };
    }

    // Ties on the receipt date go to the max PurchaseKey so the result is deterministic.
    private static int LastReceiptQuantity(List<PurchaseLine> lines)
    {
        var latest = lines
            .GroupBy(line => line.PurchaseKey)
            .OrderByDescending(g => g.Min(line => line.PurchasedAt))
            .ThenByDescending(g => g.Key)
            .First();
        return latest.Sum(line => line.Quantity);
    }

    // Receipt date = earliest PurchasedAt per receipt; instants are UTC so DST does not skew the span.
    // Breaks between classes are not purchasing time; receipts inside a break still count as points.
    private static decimal? AverageIntervalDays(
        List<PurchaseLine> lines,
        int receipts,
        TimeZoneInfo timeZone,
        IReadOnlyCollection<ClassPeriod> classPeriods)
    {
        if (receipts < 3)
        {
            return null;
        }

        var dates = lines.GroupBy(line => line.PurchaseKey).Select(g => g.Min(line => line.PurchasedAt)).ToList();
        var first = dates.Min();
        var last = dates.Max();
        var spanDays = (decimal)(last - first - ClassBreaks.Between(classPeriods, first, last, timeZone)).TotalDays;
        return Math.Round(spanDays / (receipts - 1), 2);
    }
}
