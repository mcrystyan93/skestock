namespace skestock.Application.Features.Statistics.Queries.GetPurchaseSegments;

// Internal to the classifier (no longer an API contract).
public record PurchaseSegmentItemDto
{
    public Guid ItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public Guid? ImageFileId { get; init; }
    public string Unit { get; init; } = string.Empty;
    public decimal AverageQuantity { get; init; }
    public int PurchaseCount { get; init; }
    public decimal? AverageIntervalDays { get; init; }
}

public record PurchaseSegmentsDto
{
    public List<PurchaseSegmentItemDto> Weekly { get; init; } = [];
    public List<PurchaseSegmentItemDto> EveryTwoWeeks { get; init; } = [];
    public List<PurchaseSegmentItemDto> Monthly { get; init; } = [];
    public List<PurchaseSegmentItemDto> OncePerClass { get; init; } = [];
}

// One item's purchase figures: the merged item DTO (never null), when its Last365Days row was last
// purchased (null when it has none) and its per-class purchase counts.
public sealed record PurchaseSegmentCandidate(
    PurchaseSegmentItemDto Item,
    DateTimeOffset? LastPurchasedAt,
    IReadOnlyCollection<int> ClassPurchaseCounts);

public static class PurchaseSegmentClassifier
{
    private const decimal WeeklyMaxDays = 10m;
    private const decimal EveryTwoWeeksMaxDays = 21m;
    private const decimal MonthlyMaxDays = 45m;
    private const int MinClasses = 2;
    private const decimal OncePerClassShare = 0.8m;

    // Segments are global across classes. OncePerClass uses all-time per-class counts (all classes, closed
    // ones included) and takes precedence over cadence.
    public static PurchaseSegmentsDto Classify(IEnumerable<PurchaseSegmentCandidate> candidates, DateTimeOffset now)
    {
        var weekly = new List<PurchaseSegmentItemDto>();
        var everyTwoWeeks = new List<PurchaseSegmentItemDto>();
        var monthly = new List<PurchaseSegmentItemDto>();
        var oncePerClass = new List<PurchaseSegmentItemDto>();

        foreach (var candidate in candidates)
        {
            var item = candidate.Item;
            var counts = candidate.ClassPurchaseCounts;

            if (counts.Count >= MinClasses && counts.Count(c => c == 1) >= OncePerClassShare * counts.Count)
            {
                oncePerClass.Add(item);
            }
            // Interval <= 0 is not a cadence; items with no purchase within 2x their interval are dropped.
            else if (item.AverageIntervalDays is > 0 and var days
                     && candidate.LastPurchasedAt is { } last && (now - last).TotalDays <= 2 * (double)days)
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
