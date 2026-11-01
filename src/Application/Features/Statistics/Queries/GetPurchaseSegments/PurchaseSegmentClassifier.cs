using skestock.Application.Features.Statistics.Models;

namespace skestock.Application.Features.Statistics.Queries.GetPurchaseSegments;

// One item's purchase figures: its Last365Days row (null when it has none) and its per-class rows.
public sealed record PurchaseSegmentCandidate(
    PurchaseSegmentItemDto Year,
    IReadOnlyCollection<int> ClassPurchaseCounts);

public static class PurchaseSegmentClassifier
{
    public const decimal WeeklyMaxDays = 10m;
    public const decimal EveryTwoWeeksMaxDays = 21m;
    public const decimal MonthlyMaxDays = 45m;
    public const int MinClasses = 2;
    public const decimal OncePerClassShare = 0.8m;

    public static PurchaseSegmentsDto Classify(IEnumerable<PurchaseSegmentCandidate> candidates)
    {
        var weekly = new List<PurchaseSegmentItemDto>();
        var everyTwoWeeks = new List<PurchaseSegmentItemDto>();
        var monthly = new List<PurchaseSegmentItemDto>();
        var oncePerClass = new List<PurchaseSegmentItemDto>();

        foreach (var candidate in candidates)
        {
            var item = candidate.Year;
            var counts = candidate.ClassPurchaseCounts;

            if (counts.Count >= MinClasses && counts.Count(c => c == 1) >= OncePerClassShare * counts.Count)
            {
                oncePerClass.Add(item);
            }
            else if (item.AverageIntervalDays is { } days)
            {
                if (days <= WeeklyMaxDays) weekly.Add(item);
                else if (days <= EveryTwoWeeksMaxDays) everyTwoWeeks.Add(item);
                else if (days <= MonthlyMaxDays) monthly.Add(item);
            }
        }

        return new PurchaseSegmentsDto
        {
            Weekly = ByName(weekly),
            EveryTwoWeeks = ByName(everyTwoWeeks),
            Monthly = ByName(monthly),
            OncePerClass = ByName(oncePerClass)
        };
    }

    private static List<PurchaseSegmentItemDto> ByName(List<PurchaseSegmentItemDto> items) =>
        items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();
}
