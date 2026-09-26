using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Statistics.Commands.MaterializeDailyConsumption;

namespace skestock.Application.UnitTests.Features.Statistics.Commands.MaterializeDailyConsumption;

[TestFixture]
public sealed class MaterializeDailyConsumptionCommandValidatorTests
{
    private readonly MaterializeDailyConsumptionCommandValidator _validator = new();

    [Test]
    public void Valid_range_passes()
    {
        var result = _validator.Validate(Command(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 26)));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void Range_of_max_days_passes()
    {
        var from = new DateOnly(2026, 9, 1);
        var result = _validator.Validate(
            Command(from, from.AddDays(MaterializeDailyConsumptionCommand.MaxDays - 1)));

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void Range_longer_than_max_days_fails()
    {
        var from = new DateOnly(2026, 9, 1);
        var result = _validator.Validate(
            Command(from, from.AddDays(MaterializeDailyConsumptionCommand.MaxDays)));

        result.IsValid.ShouldBeFalse();
    }

    [Test]
    public void ToDate_before_FromDate_fails()
    {
        var result = _validator.Validate(Command(new DateOnly(2026, 9, 26), new DateOnly(2026, 9, 25)));

        result.Errors.ShouldContain(error =>
            error.PropertyName == nameof(MaterializeDailyConsumptionCommand.ToDate));
    }

    [TestCase("")]
    [TestCase("Not/AZone")]
    public void Unknown_time_zone_fails(string timeZoneId)
    {
        var result = _validator.Validate(
            Command(new DateOnly(2026, 9, 26), new DateOnly(2026, 9, 26), timeZoneId));

        result.Errors.ShouldContain(error =>
            error.PropertyName == nameof(MaterializeDailyConsumptionCommand.TimeZoneId));
    }

    private static MaterializeDailyConsumptionCommand Command(
        DateOnly from,
        DateOnly to,
        string timeZoneId = "Europe/Bucharest") => new()
    {
        FromDate = from,
        ToDate = to,
        TimeZoneId = timeZoneId
    };
}
