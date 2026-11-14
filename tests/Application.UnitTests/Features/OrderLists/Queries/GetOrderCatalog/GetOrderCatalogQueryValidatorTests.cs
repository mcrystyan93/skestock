using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.OrderLists.Queries.GetOrderCatalog;

namespace skestock.Application.UnitTests.Features.OrderLists.Queries.GetOrderCatalog;

[TestFixture]
public sealed class GetOrderCatalogQueryValidatorTests
{
    private readonly GetOrderCatalogQueryValidator _validator = new();

    [Test]
    public void Empty_class_id_is_rejected()
    {
        var result = _validator.Validate(new GetOrderCatalogQuery { ClassId = Guid.Empty });

        result.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(ValidationErrorCodes.Required);
    }

    [Test]
    public void Class_id_is_accepted()
    {
        _validator.Validate(new GetOrderCatalogQuery { ClassId = Guid.NewGuid() }).IsValid.ShouldBeTrue();
    }
}
