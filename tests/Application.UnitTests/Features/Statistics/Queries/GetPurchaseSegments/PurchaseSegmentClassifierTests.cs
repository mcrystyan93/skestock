using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Queries.GetPurchaseSegments;

namespace skestock.Application.UnitTests.Features.Statistics.Queries.GetPurchaseSegments;

[TestFixture]
public sealed class PurchaseSegmentClassifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    private static PurchaseSegmentCandidate Year(
        string name, decimal? interval, int daysSinceLast = 0, params int[] classCounts) =>
        new(new PurchaseSegmentItemDto { ItemId = Guid.NewGuid(), Name = name, AverageIntervalDays = interval },
            Now.AddDays(-daysSinceLast), classCounts);

    private static PurchaseSegmentCandidate PerClass(string name, params int[] classCounts) =>
        new(new PurchaseSegmentItemDto { ItemId = Guid.NewGuid(), Name = name }, null, classCounts);

    private static PurchaseSegmentsDto Run(params PurchaseSegmentCandidate[] candidates) =>
        PurchaseSegmentClassifier.Classify(candidates, Now);

    private static int Total(PurchaseSegmentsDto dto) =>
        dto.Weekly.Count + dto.EveryTwoWeeks.Count + dto.Monthly.Count + dto.OncePerClass.Count;

    [TestCase(0.01, "Weekly")]
    [TestCase(10, "Weekly")]
    [TestCase(10.01, "EveryTwoWeeks")]
    [TestCase(21, "EveryTwoWeeks")]
    [TestCase(21.01, "Monthly")]
    [TestCase(45, "Monthly")]
    [TestCase(45.01, "None")]
    public void Cadence_boundaries(double days, string expected)
    {
        var dto = Run(Year("A", (decimal)days));

        var lists = new Dictionary<string, List<PurchaseSegmentItemDto>>
        {
            ["Weekly"] = dto.Weekly, ["EveryTwoWeeks"] = dto.EveryTwoWeeks, ["Monthly"] = dto.Monthly
        };
        foreach (var (name, list) in lists)
            list.Count.ShouldBe(name == expected ? 1 : 0, name);
        dto.OncePerClass.ShouldBeEmpty();
    }

    [Test]
    public void Dormant_item_and_zero_interval_item_are_excluded_but_item_within_tolerance_is_kept()
    {
        var dto = Run(Year("Dormant", 5m, 11), Year("SameDay", 0m), Year("Tolerated", 5m, 10));

        dto.Weekly.Select(i => i.Name).ShouldBe(["Tolerated"]);
        (dto.EveryTwoWeeks.Count + dto.Monthly.Count + dto.OncePerClass.Count).ShouldBe(0);
    }

    [TestCase(14, 28, "EveryTwoWeeks", true)]
    [TestCase(14, 29, "EveryTwoWeeks", false)]
    [TestCase(30, 60, "Monthly", true)]
    [TestCase(30, 61, "Monthly", false)]
    [TestCase(21, 42, "EveryTwoWeeks", true)]
    [TestCase(45, 90, "Monthly", true)]
    public void Dormancy_cutoff_is_twice_the_interval(int interval, int daysSinceLast, string segment, bool kept)
    {
        var dto = Run(Year("A", interval, daysSinceLast));

        (segment == "Monthly" ? dto.Monthly : dto.EveryTwoWeeks).Count.ShouldBe(kept ? 1 : 0);
        Total(dto).ShouldBe(kept ? 1 : 0);
    }

    [Test]
    public void Dormant_item_that_qualifies_for_once_per_class_is_still_listed()
    {
        var dto = Run(Year("A", 5m, 400, 1, 1));

        dto.OncePerClass.Select(i => i.Name).ShouldBe(["A"]);
        dto.Weekly.ShouldBeEmpty();
    }

    [TestCase(-5)]
    [TestCase(0)]
    public void Non_positive_interval_lands_in_no_segment(int interval)
    {
        Total(Run(Year("A", interval))).ShouldBe(0);
    }

    [Test]
    public void Null_interval_appears_nowhere()
    {
        Total(Run(Year("A", null))).ShouldBe(0);
    }

    [Test]
    public void Per_class_takes_precedence_over_cadence()
    {
        var dto = Run(Year("A", 5m, 0, 1, 1));

        dto.Weekly.ShouldBeEmpty();
        dto.OncePerClass.ShouldHaveSingleItem();
    }

    [Test]
    public void Eighty_percent_rule_four_of_five_in_three_of_five_out()
    {
        var dto = Run(PerClass("Four", 1, 1, 1, 1, 3), PerClass("Three", 1, 1, 1, 3, 3));

        dto.OncePerClass.Select(i => i.Name).ShouldBe(["Four"]);
    }

    [Test]
    public void Single_class_is_not_once_per_class()
    {
        Run(PerClass("A", 1)).OncePerClass.ShouldBeEmpty();
    }

    [Test]
    public void Lists_are_sorted_by_name()
    {
        Run(Year("Zahar", 5m), Year("Apa", 5m)).Weekly.Select(i => i.Name).ShouldBe(["Apa", "Zahar"]);
    }
}
