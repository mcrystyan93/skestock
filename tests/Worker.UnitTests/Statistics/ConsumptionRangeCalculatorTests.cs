using NUnit.Framework;
using Shouldly;
using Worker.Statistics;

namespace Worker.UnitTests.Statistics;

[TestFixture]
public sealed class ConsumptionRangeCalculatorTests
{
    private static readonly TimeZoneInfo Bucharest =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest");

    // 2026-09-26 21:00 local (EEST, UTC+3).
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 18, 0, 0, TimeSpan.Zero);

    [Test]
    public void Normal_run_recomputes_yesterday_and_today()
    {
        var lastSucceeded = new DateTimeOffset(2026, 9, 25, 18, 0, 5, TimeSpan.Zero);

        var (from, to) = ConsumptionRangeCalculator.Calculate(Now, lastSucceeded, Bucharest, 31);

        from.ShouldBe(new DateOnly(2026, 9, 24));
        to.ShouldBe(new DateOnly(2026, 9, 26));
    }

    [Test]
    public void Late_night_success_uses_the_local_date()
    {
        // 2026-09-25 22:30Z is already 2026-09-26 01:30 in Bucharest.
        var lastSucceeded = new DateTimeOffset(2026, 9, 25, 22, 30, 0, TimeSpan.Zero);

        var (from, _) = ConsumptionRangeCalculator.Calculate(Now, lastSucceeded, Bucharest, 31);

        from.ShouldBe(new DateOnly(2026, 9, 25));
    }

    [Test]
    public void First_run_covers_max_days()
    {
        var (from, to) = ConsumptionRangeCalculator.Calculate(Now, null, Bucharest, 31);

        from.ShouldBe(new DateOnly(2026, 8, 27));
        to.ShouldBe(new DateOnly(2026, 9, 26));
    }

    [Test]
    public void Long_outage_is_capped_to_max_days()
    {
        var lastSucceeded = new DateTimeOffset(2026, 5, 1, 18, 0, 0, TimeSpan.Zero);

        var (from, to) = ConsumptionRangeCalculator.Calculate(Now, lastSucceeded, Bucharest, 31);

        (to.DayNumber - from.DayNumber + 1).ShouldBe(31);
    }

    [Test]
    public void Utc_date_differs_from_local_date_near_midnight()
    {
        // 2026-09-26 22:30Z is 2026-09-27 01:30 local.
        var now = new DateTimeOffset(2026, 9, 26, 22, 30, 0, TimeSpan.Zero);

        var (_, to) = ConsumptionRangeCalculator.Calculate(now, null, Bucharest, 31);

        to.ShouldBe(new DateOnly(2026, 9, 27));
    }
}
