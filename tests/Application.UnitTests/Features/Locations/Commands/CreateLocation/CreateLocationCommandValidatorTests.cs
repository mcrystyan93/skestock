using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Locations.Commands.CreateLocation;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Commands.CreateLocation;

public class CreateLocationCommandValidatorTests
{
    private static LocationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationTestDbContext(options);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenNameAndTypeAreValidAndUnique()
    {
        await using var context = CreateContext();
        var validator = new CreateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Kitchen", Type = "Kitchen" });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new CreateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "", Type = "Kitchen" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateLocationCommand.Name) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenTypeIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new CreateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Kitchen", Type = "" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateLocationCommand.Type) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenNameExceedsMaximum()
    {
        await using var context = CreateContext();
        var validator = new CreateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = new string('a', 101), Type = "Kitchen" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateLocationCommand.Name) && e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldHaveMaxLengthErrorWhenTypeExceedsMaximum()
    {
        await using var context = CreateContext();
        var validator = new CreateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Kitchen", Type = new string('a', 51) });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateLocationCommand.Type) && e.ErrorCode == ValidationErrorCodes.MaxLength);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameAlreadyExists()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Name = "Kitchen", Type = "Kitchen" });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Kitchen", Type = "StorageRoom" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameDiffersOnlyByCaseOrWhitespace()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Name = "Kitchen", Type = "Kitchen" });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "  KITCHEN  ", Type = "StorageRoom" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenNameIsUniqueAmongExistingLocations()
    {
        await using var context = CreateContext();
        context.Locations.Add(new Location { Name = "Kitchen", Type = "Kitchen" });
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Gym", Type = "Gym" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenParentLocationIdExists()
    {
        await using var context = CreateContext();
        var parent = new Location { Name = "Main Building", Type = "Building" };
        context.Locations.Add(parent);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new CreateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Room 101", Type = "Room", ParentLocationId = parent.Id });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenParentLocationIdDoesNotExist()
    {
        await using var context = CreateContext();
        var validator = new CreateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Room 101", Type = "Room", ParentLocationId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldNotHaveInvalidReferenceErrorWhenParentLocationIdIsNull()
    {
        await using var context = CreateContext();
        var validator = new CreateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateLocationCommand { Name = "Room 101", Type = "Room", ParentLocationId = null });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }
}
