using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Commands.MaterializePurchaseStatistics;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Statistics.Commands.MaterializePurchaseStatistics;

[TestFixture]
public sealed class PurchaseStatisticsCalculatorTests
{
    private static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");
    private static readonly DateOnly Today = new(2026, 9, 26);
    private static readonly DateTimeOffset ComputedAt = new(2026, 9, 26, 18, 0, 0, TimeSpan.Zero);
    private static readonly Guid ItemA = Guid.NewGuid();
    private static readonly Guid ItemB = Guid.NewGuid();
    private static readonly Guid ClassA = Guid.NewGuid();
    private static readonly Guid ClassB = Guid.NewGuid();

    private static PurchaseLine Line(
        Guid item, Guid schoolClass, Guid key, int quantity, decimal price, DateTimeOffset at) =>
        new(item, schoolClass, key, quantity, price, at);

    private static decimal? Interval(PurchaseStatisticsScope scope, params PurchaseLine[] lines) =>
        PurchaseStatisticsCalculator.Calculate(lines, Today, TimeZone, ComputedAt)
            .Single(s => s.Scope == scope && s.ClassId == null).AverageIntervalDays;

    private static PurchaseLine On(DateTimeOffset at, Guid? receipt = null) =>
        Line(ItemA, ClassA, receipt ?? Guid.NewGuid(), 1, 1m, at);

    private static readonly DateTimeOffset Day0 = new(2026, 9, 1, 9, 0, 0, TimeSpan.Zero);

    [Test]
    public void Interval_is_null_below_three_receipts()
    {
        Interval(PurchaseStatisticsScope.Last365Days, On(Day0)).ShouldBeNull();
        Interval(PurchaseStatisticsScope.Last365Days, On(Day0), On(Day0.AddDays(7))).ShouldBeNull();
    }

    [Test]
    public void Interval_is_span_divided_by_receipts_minus_one()
    {
        Interval(PurchaseStatisticsScope.Last365Days, On(Day0), On(Day0.AddDays(3)), On(Day0.AddDays(10)))
            .ShouldBe(5m);
        Interval(PurchaseStatisticsScope.Last365Days, On(Day0), On(Day0.AddDays(1)), On(Day0.AddDays(3)),
            On(Day0.AddDays(4))).ShouldBe(1.33m);
    }

    [Test]
    public void Interval_counts_lines_on_one_receipt_once()
    {
        var shared = Guid.NewGuid();
        Interval(PurchaseStatisticsScope.Last365Days,
            On(Day0, shared), On(Day0.AddHours(1), shared), On(Day0.AddDays(5)), On(Day0.AddDays(10)))
            .ShouldBe(5m);
    }

    [Test]
    public void Interval_is_zero_for_same_day_receipts()
    {
        Interval(PurchaseStatisticsScope.Last365Days, On(Day0), On(Day0), On(Day0)).ShouldBe(0m);
    }

    [Test]
    public void Interval_uses_utc_instants_across_a_dst_change()
    {
        // Europe/Bucharest leaves DST on 2026-10-25; 10 UTC days still span exactly 10 days.
        var start = new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);
        Interval(PurchaseStatisticsScope.Last365Days, On(start), On(start.AddDays(5)), On(start.AddDays(10)))
            .ShouldBe(5m);
    }

    [Test]
    public void Interval_is_only_set_on_the_365_day_scope()
    {
        var lines = new[] { On(Day0), On(Day0.AddDays(3)), On(Day0.AddDays(10)) };
        Interval(PurchaseStatisticsScope.Last90Days, lines).ShouldBeNull();
        PurchaseStatisticsCalculator.Calculate(lines, Today, TimeZone, ComputedAt)
            .Single(s => s.Scope == PurchaseStatisticsScope.Class).AverageIntervalDays.ShouldBeNull();
    }

    [Test]
    public void Lines_on_the_same_receipt_count_as_one_purchase()
    {
        var receipt = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 9, 20, 9, 0, 0, TimeSpan.Zero);
        var lines = new[]
        {
            Line(ItemA, ClassA, receipt, 10, 2m, at),
            Line(ItemA, ClassA, receipt, 5, 4m, at),
            Line(ItemA, ClassA, Guid.NewGuid(), 15, 2m, at.AddDays(1))
        };

        var statistic = PurchaseStatisticsCalculator.Calculate(lines, Today, TimeZone, ComputedAt)
            .Single(s => s.Scope == PurchaseStatisticsScope.Last90Days);

        statistic.ItemId.ShouldBe(ItemA);
        statistic.ClassId.ShouldBeNull();
        statistic.TotalQuantity.ShouldBe(30);
        statistic.TotalValue.ShouldBe(70m);
        statistic.PurchaseCount.ShouldBe(2);
        statistic.AverageQuantity.ShouldBe(15m);
        statistic.AverageUnitPrice.ShouldBe(2.33m);
        statistic.LastPurchasedAt.ShouldBe(at.AddDays(1));
        statistic.ComputedAt.ShouldBe(ComputedAt);
    }

    [Test]
    public void Rolling_windows_start_at_local_midnight()
    {
        // 2026-06-29 00:00 in Bucharest (UTC+3) is the first day of the 90-day window.
        var windowStartUtc = new DateTimeOffset(2026, 6, 28, 21, 0, 0, TimeSpan.Zero);
        var lines = new[]
        {
            Line(ItemA, ClassA, Guid.NewGuid(), 1, 1m, windowStartUtc),
            Line(ItemA, ClassA, Guid.NewGuid(), 2, 1m, windowStartUtc.AddTicks(-1)),
            Line(ItemA, ClassA, Guid.NewGuid(), 4, 1m, new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero))
        };

        var statistics = PurchaseStatisticsCalculator.Calculate(lines, Today, TimeZone, ComputedAt);

        statistics.Single(s => s.Scope == PurchaseStatisticsScope.Last90Days).TotalQuantity.ShouldBe(1);
        statistics.Single(s => s.Scope == PurchaseStatisticsScope.Last365Days).TotalQuantity.ShouldBe(3);
        statistics.Single(s => s.Scope == PurchaseStatisticsScope.Class).TotalQuantity.ShouldBe(7);
    }

    [Test]
    public void Class_scope_is_grouped_per_class_and_item()
    {
        var at = new DateTimeOffset(2026, 9, 20, 9, 0, 0, TimeSpan.Zero);
        var lines = new[]
        {
            Line(ItemA, ClassA, Guid.NewGuid(), 1, 1m, at),
            Line(ItemA, ClassB, Guid.NewGuid(), 2, 1m, at),
            Line(ItemB, ClassA, Guid.NewGuid(), 3, 1m, at)
        };

        var statistics = PurchaseStatisticsCalculator.Calculate(lines, Today, TimeZone, ComputedAt);

        var classRows = statistics.Where(s => s.Scope == PurchaseStatisticsScope.Class).ToList();
        classRows.Count.ShouldBe(3);
        classRows.Single(s => s.ClassId == ClassB && s.ItemId == ItemA).TotalQuantity.ShouldBe(2);
        statistics.Single(s => s.Scope == PurchaseStatisticsScope.Last365Days && s.ItemId == ItemA)
            .TotalQuantity.ShouldBe(3);
    }

    [Test]
    public void No_lines_produce_no_rows()
    {
        PurchaseStatisticsCalculator.Calculate([], Today, TimeZone, ComputedAt).ShouldBeEmpty();
    }
}
