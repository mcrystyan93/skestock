using skestock.Application.Features.Locations.Queries.GetDefaultLocation;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Queries.GetDefaultLocation;

public class GetDefaultLocationQueryValidatorTests
{
    private readonly GetDefaultLocationQueryValidator _validator = new();

    [Test]
    public async Task ShouldBeValid_WhenQueryHasNoInput()
    {
        var result = await _validator.ValidateAsync(new GetDefaultLocationQuery());

        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }
}
