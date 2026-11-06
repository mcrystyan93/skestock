using NUnit.Framework;
using Shouldly;
using skestock.Infrastructure.FruitDessert;

namespace skestock.Application.UnitTests.Infrastructure.FruitDessert;

public class WorkingDayProviderTests
{
    private readonly WorkingDayProvider _provider = new();

    [Test]
    public void GetWorkingDays_ReturnsMondayToFridayAcrossTwoWeeks()
    {
        // 2026-11-02 is a Monday.
        var days = _provider.GetWorkingDays(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 13)).ToList();

        days.Count.ShouldBe(10);
        days.ShouldNotContain(new DateOnly(2026, 11, 7));
        days.ShouldNotContain(new DateOnly(2026, 11, 8));
        days.First().ShouldBe(new DateOnly(2026, 11, 2));
        days.Last().ShouldBe(new DateOnly(2026, 11, 13));
    }

    [Test]
    public void GetWorkingDays_WeekendBoundaries_AreSkipped()
    {
        // Saturday to Sunday of the following week: only Monday to Friday remain.
        var days = _provider.GetWorkingDays(new DateOnly(2026, 11, 7), new DateOnly(2026, 11, 15)).ToList();

        days.ShouldBe(Enumerable.Range(9, 5).Select(d => new DateOnly(2026, 11, d)));
    }

    [Test]
    public void GetWorkingDays_SingleWeekday_ReturnsThatDay()
    {
        var day = new DateOnly(2026, 11, 4);

        _provider.GetWorkingDays(day, day).ShouldBe([day]);
    }

    [Test]
    public void GetWorkingDays_SingleWeekendDay_ReturnsEmpty()
    {
        var saturday = new DateOnly(2026, 11, 7);

        _provider.GetWorkingDays(saturday, saturday).ShouldBeEmpty();
    }

    [Test]
    public void GetWorkingDays_InvertedInterval_ReturnsEmpty()
    {
        _provider.GetWorkingDays(new DateOnly(2026, 11, 13), new DateOnly(2026, 11, 2)).ShouldBeEmpty();
    }
}
