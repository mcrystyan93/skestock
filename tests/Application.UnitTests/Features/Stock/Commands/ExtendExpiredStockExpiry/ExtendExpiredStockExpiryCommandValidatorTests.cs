using skestock.Application.Features.Stock.Commands.ExtendExpiredStockExpiry;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Stock.Commands.ExtendExpiredStockExpiry;

public class ExtendExpiredStockExpiryCommandValidatorTests
{
    private readonly ExtendExpiredStockExpiryCommandValidator _validator = new();

    [TestCase(1, true)]
    [TestCase(365, true)]
    [TestCase(0, false)]
    [TestCase(-3, false)]
    [TestCase(366, false)]
    public void Validate_ExtensionDays(int days, bool valid)
    {
        var result = _validator.Validate(Create(days, Guid.NewGuid()));
        result.IsValid.ShouldBe(valid);
    }

    [Test]
    public void Validate_EmptyIds_Fails()
    {
        _validator.Validate(Create(7, Guid.Empty)).IsValid.ShouldBeFalse();
    }

    private static ExtendExpiredStockExpiryCommand Create(int days, Guid id) => new()
    {
        ClassId = id,
        ItemId = id,
        LocationId = id,
        ExtensionDays = days
    };
}
