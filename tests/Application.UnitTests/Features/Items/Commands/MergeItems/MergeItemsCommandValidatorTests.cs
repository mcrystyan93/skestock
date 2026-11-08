using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.MergeItems;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.MergeItems;

public class MergeItemsCommandValidatorTests
{
    private readonly MergeItemsCommandValidator _validator = new();

    [Test]
    public async Task ShouldHaveErrorWhenIdsAreEmpty()
    {
        var result = await _validator.ValidateAsync(new MergeItemsCommand(Guid.NewGuid(), []));

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveErrorWhenOnlyOneId()
    {
        var id = Guid.NewGuid();

        var result = await _validator.ValidateAsync(new MergeItemsCommand(id, [id]));

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [Test]
    public async Task ShouldHaveErrorWhenDuplicatesCollapseToOne()
    {
        var id = Guid.NewGuid();

        var result = await _validator.ValidateAsync(new MergeItemsCommand(id, [id, id]));

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.GreaterThanOrEqualTo);
    }

    [Test]
    public async Task ShouldHaveErrorWhenPrimaryNotInSet()
    {
        var result = await _validator.ValidateAsync(
            new MergeItemsCommand(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()]));

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldBeValidForTwoDistinctIdsIncludingPrimary()
    {
        var primary = Guid.NewGuid();

        var result = await _validator.ValidateAsync(new MergeItemsCommand(primary, [primary, Guid.NewGuid()]));

        result.IsValid.ShouldBeTrue();
    }
}
