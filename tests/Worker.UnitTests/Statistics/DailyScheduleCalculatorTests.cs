using NUnit.Framework;
using Shouldly;
using Worker.Statistics;

namespace Worker.UnitTests.Statistics;

[TestFixture]
public sealed class DailyScheduleCalculatorTests
{
    private static readonly TimeZoneInfo Bucharest =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");

    [Test]
    public void BeforeRunTime_ReturnsTodayAtNinePmBucharest()
    {
        var calculator = new DailyScheduleCalculator(new TimeOnly(21, 0), Bucharest);
        var now = new DateTimeOffset(2026, 9, 25, 17, 0, 0, TimeSpan.Zero);

        calculator.GetNextOccurrence(now)
            .ShouldBe(new DateTimeOffset(2026, 9, 25, 18, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void AfterRunTime_ReturnsTheNextDayAtNinePmBucharest()
    {
        var calculator = new DailyScheduleCalculator(new TimeOnly(21, 0), Bucharest);
        var now = new DateTimeOffset(2026, 9, 25, 18, 30, 0, TimeSpan.Zero);

        calculator.GetMostRecentDueOccurrence(now)
            .ShouldBe(new DateTimeOffset(2026, 9, 25, 18, 0, 0, TimeSpan.Zero));
        calculator.GetNextOccurrence(now)
            .ShouldBe(new DateTimeOffset(2026, 9, 26, 18, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void Schedule_TracksSpringDstChange()
    {
        var calculator = new DailyScheduleCalculator(new TimeOnly(21, 0), Bucharest);
        var now = new DateTimeOffset(2026, 3, 28, 19, 30, 0, TimeSpan.Zero);

        calculator.GetNextOccurrence(now)
            .ShouldBe(new DateTimeOffset(2026, 3, 29, 18, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void Schedule_TracksAutumnDstChange()
    {
        var calculator = new DailyScheduleCalculator(new TimeOnly(21, 0), Bucharest);
        var now = new DateTimeOffset(2026, 10, 24, 18, 30, 0, TimeSpan.Zero);

        calculator.GetNextOccurrence(now)
            .ShouldBe(new DateTimeOffset(2026, 10, 25, 19, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void InvalidLocalTime_IsMovedToTheFirstValidMinute()
    {
        var calculator = new DailyScheduleCalculator(new TimeOnly(3, 30), Bucharest);
        var now = new DateTimeOffset(2026, 3, 28, 21, 0, 0, TimeSpan.Zero);

        var occurrence = calculator.GetNextOccurrence(now);
        var localOccurrence = TimeZoneInfo.ConvertTime(occurrence, Bucharest);

        localOccurrence.DateTime.ShouldBe(
            new DateTime(2026, 3, 29, 4, 0, 0, DateTimeKind.Unspecified));
    }

    [Test]
    public void AmbiguousLocalTime_ResolvesToOneStableUtcOccurrence()
    {
        var calculator = new DailyScheduleCalculator(new TimeOnly(3, 30), Bucharest);
        var now = new DateTimeOffset(2026, 10, 24, 21, 0, 0, TimeSpan.Zero);

        var occurrence = calculator.GetNextOccurrence(now);
        var localOccurrence = TimeZoneInfo.ConvertTime(occurrence, Bucharest);

        occurrence.Offset.ShouldBe(TimeSpan.Zero);
        localOccurrence.DateTime.ShouldBe(
            new DateTime(2026, 10, 25, 3, 30, 0, DateTimeKind.Unspecified));
    }
}
