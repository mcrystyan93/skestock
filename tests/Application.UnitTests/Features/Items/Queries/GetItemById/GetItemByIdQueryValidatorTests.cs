using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Queries.GetItemById;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetItemById;

public class GetItemByIdQueryValidatorTests
{
    private readonly GetItemByIdQueryValidator _validator = new();

    [Test]
    public async Task ShouldNotHaveErrorWhenIdIsNotEmpty()
    {
        var result = await _validator.ValidateAsync(new GetItemByIdQuery { Id = Guid.NewGuid() });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        var result = await _validator.ValidateAsync(new GetItemByIdQuery { Id = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }
}
