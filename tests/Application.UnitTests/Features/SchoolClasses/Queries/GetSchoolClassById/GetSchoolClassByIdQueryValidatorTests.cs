using skestock.Application.Common.Errors;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Queries.GetSchoolClassById;

public class GetSchoolClassByIdQueryValidatorTests
{
    private readonly GetSchoolClassByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsNotEmpty()
    {
        var result = await _validator.ValidateAsync(new GetSchoolClassByIdQuery { Id = Guid.NewGuid() });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new GetSchoolClassByIdQuery { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }
}
