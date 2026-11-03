using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics;

namespace skestock.Application.UnitTests.Features.Statistics;

[TestFixture]
public sealed class ClassBreaksTests
{
    private static readonly TimeZoneInfo Bucharest =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");

    private static readonly ClassPeriod A = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
    // 14 calendar days after A ends.
    private static readonly ClassPeriod B = new(new DateOnly(2026, 2, 15), new DateOnly(2026, 3, 31));

    private static DateTimeOffset Utc(int month, int day, int hour = 0) =>
        new(2026, month, day, hour, 0, 0, TimeSpan.Zero);

    private static TimeSpan Between(ClassPeriod[] periods, DateTimeOffset from, DateTimeOffset to) =>
        ClassBreaks.Between(periods, from, to, Bucharest);

    [Test]
    public void No_periods_have_no_break()
    {
        Between([], Utc(1, 1), Utc(12, 31)).ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void Single_class_has_no_break()
    {
        Between([A], Utc(1, 1), Utc(12, 31)).ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void Time_outside_the_first_start_and_last_end_is_not_a_break()
    {
        Between([A, B], Utc(1, 1), Utc(4, 30)).ShouldBe(TimeSpan.FromDays(14));
        Between([A, B], Utc(1, 1), Utc(12, 31)).ShouldBe(TimeSpan.FromDays(14));
    }

    [Test]
    public void Empty_or_reversed_range_is_zero()
    {
        Between([A, B], Utc(2, 5), Utc(2, 5)).ShouldBe(TimeSpan.Zero);
        Between([A, B], Utc(3, 1), Utc(1, 10)).ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void Gap_between_two_classes_is_the_break()
    {
        Between([A, B], Utc(1, 10), Utc(3, 1)).ShouldBe(TimeSpan.FromDays(14));
    }

    [Test]
    public void Range_fully_inside_the_gap_is_all_break()
    {
        Between([A, B], Utc(2, 3, 12), Utc(2, 10, 12)).ShouldBe(TimeSpan.FromDays(7));
    }

    [Test]
    public void Range_starting_inside_the_gap_counts_only_the_remaining_gap()
    {
        // The gap ends at 2026-02-15 00:00 local = 2026-02-14 22:00 UTC.
        Between([A, B], Utc(2, 8, 12), Utc(3, 1)).ShouldBe(new TimeSpan(6, 10, 0, 0));
    }

    [Test]
    public void Overlapping_classes_merge_before_finding_gaps()
    {
        var overlapping = new ClassPeriod(new DateOnly(2026, 1, 20), new DateOnly(2026, 2, 10));
        var c = new ClassPeriod(new DateOnly(2026, 2, 24), new DateOnly(2026, 3, 31));

        Between([A, overlapping, c], Utc(1, 1), Utc(3, 31)).ShouldBe(TimeSpan.FromDays(13));
    }

    [Test]
    public void Adjacent_classes_have_no_break()
    {
        var next = new ClassPeriod(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28));

        Between([A, next], Utc(1, 1), Utc(2, 28)).ShouldBe(TimeSpan.Zero);
    }

    [Test]
    public void Input_order_does_not_matter()
    {
        Between([B, A], Utc(1, 10), Utc(3, 1)).ShouldBe(TimeSpan.FromDays(14));
    }

    [Test]
    public void Break_across_the_end_of_dst_is_one_hour_longer_in_utc()
    {
        // Gap = 2026-10-19 00:00 (UTC+3) to 2026-11-01 00:00 (UTC+2) local = 13 days + 1 hour.
        var autumn = new ClassPeriod(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 18));
        var winter = new ClassPeriod(new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 20));

        Between([autumn, winter], Utc(9, 1), Utc(12, 20)).ShouldBe(new TimeSpan(13, 1, 0, 0));
    }
}
