using skestock.Application.Common.Errors;
using skestock.Application.Features.Locations.Queries.GetLocationById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdQueryValidatorTests
{
    private readonly GetLocationByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsGreaterThanZero()
    {
        var result = await _validator.ValidateAsync(new GetLocationByIdQuery { Id = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsZero()
    {
        var result = await _validator.ValidateAsync(new GetLocationByIdQuery { Id = 0 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsNegative()
    {
        var result = await _validator.ValidateAsync(new GetLocationByIdQuery { Id = -1 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
