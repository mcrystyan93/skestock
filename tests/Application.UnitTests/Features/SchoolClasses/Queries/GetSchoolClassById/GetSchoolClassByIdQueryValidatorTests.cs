using skestock.Application.Common.Errors;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Queries.GetSchoolClassById;

public class GetSchoolClassByIdQueryValidatorTests
{
    private readonly GetSchoolClassByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsGreaterThanZero()
    {
        var result = await _validator.ValidateAsync(new GetSchoolClassByIdQuery { Id = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsZeroOrNegative()
    {
        var result = await _validator.ValidateAsync(new GetSchoolClassByIdQuery { Id = 0 });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThan);
    }
}
