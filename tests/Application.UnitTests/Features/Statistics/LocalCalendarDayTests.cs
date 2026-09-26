using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics;

namespace skestock.Application.UnitTests.Features.Statistics;

[TestFixture]
public sealed class LocalCalendarDayTests
{
    private static readonly TimeZoneInfo Bucharest =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");

    [Test]
    public void Winter_day_starts_two_hours_before_utc_midnight()
    {
        LocalCalendarDay.GetStartUtc(new DateOnly(2026, 1, 15), Bucharest)
            .ShouldBe(new DateTimeOffset(2026, 1, 14, 22, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void Summer_day_starts_three_hours_before_utc_midnight()
    {
        LocalCalendarDay.GetStartUtc(new DateOnly(2026, 7, 15), Bucharest)
            .ShouldBe(new DateTimeOffset(2026, 7, 14, 21, 0, 0, TimeSpan.Zero));
    }

    [Test]
    public void Spring_forward_day_is_23_hours_long()
    {
        var day = new DateOnly(2026, 3, 29);

        var length = LocalCalendarDay.GetStartUtc(day.AddDays(1), Bucharest)
            - LocalCalendarDay.GetStartUtc(day, Bucharest);

        length.ShouldBe(TimeSpan.FromHours(23));
    }
}
