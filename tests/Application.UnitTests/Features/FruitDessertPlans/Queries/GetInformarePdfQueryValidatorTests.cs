using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.FruitDessertPlans.Queries.GetInformarePdf;

namespace skestock.Application.UnitTests.Features.FruitDessertPlans.Queries;

public class GetInformarePdfQueryValidatorTests
{
    [Test]
    public void Validator_RejectsEmptyIds()
    {
        var result = new GetInformarePdfQueryValidator().Validate(new GetInformarePdfQuery());

        result.Errors.Select(e => e.PropertyName).ShouldBe(
            [nameof(GetInformarePdfQuery.ClassId), nameof(GetInformarePdfQuery.EntryId)], ignoreOrder: true);
    }

    [Test]
    public void Validator_AcceptsIds()
    {
        new GetInformarePdfQueryValidator()
            .Validate(new GetInformarePdfQuery { ClassId = Guid.NewGuid(), EntryId = Guid.NewGuid() })
            .IsValid.ShouldBeTrue();
    }
}
