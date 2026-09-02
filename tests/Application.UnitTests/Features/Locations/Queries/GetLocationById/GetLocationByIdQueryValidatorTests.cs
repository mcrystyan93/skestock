using skestock.Application.Common.Errors;
using skestock.Application.Features.Locations.Queries.GetLocationById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdQueryValidatorTests
{
    private readonly GetLocationByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsNotEmpty()
    {
        var result = await _validator.ValidateAsync(new GetLocationByIdQuery { Id = Guid.NewGuid() });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new GetLocationByIdQuery { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmptyVariant()
    {
        var result = await _validator.ValidateAsync(new GetLocationByIdQuery { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }
}
