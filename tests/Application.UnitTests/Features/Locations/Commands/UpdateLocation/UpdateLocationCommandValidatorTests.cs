using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Locations.Commands.UpdateLocation;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationCommandValidatorTests
{
    private static LocationTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LocationTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LocationTestDbContext(options);
    }

    [Test]
    public async Task ShouldNotHaveErrorWhenCommandIsValid()
    {
        await using var context = CreateContext();
        var location = new Location { Name = "Kitchen", Type = "Kitchen" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = location.Id, Name = "New Kitchen", Type = "Kitchen" });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveErrorWhenIdIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new UpdateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = Guid.Empty, Name = "Kitchen", Type = "Kitchen" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateLocationCommand.Id) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenNameIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new UpdateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = Guid.NewGuid(), Name = "", Type = "Kitchen" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateLocationCommand.Name) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenTypeIsEmpty()
    {
        await using var context = CreateContext();
        var validator = new UpdateLocationCommandValidator(context);

        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = Guid.NewGuid(), Name = "Kitchen", Type = "" });

        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateLocationCommand.Type) && e.ErrorCode == ValidationErrorCodes.Required);
    }

    [Test]
    public async Task ShouldNotHaveDuplicateNameErrorWhenNameBelongsToTheSameLocation()
    {
        await using var context = CreateContext();
        var location = new Location { Name = "Kitchen", Type = "Kitchen" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = location.Id, Name = "Kitchen", Type = "Kitchen" });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveDuplicateNameErrorWhenNameBelongsToAnotherLocation()
    {
        await using var context = CreateContext();
        var existing = new Location { Name = "Kitchen", Type = "Kitchen" };
        var toUpdate = new Location { Name = "Gym", Type = "Gym" };
        context.Locations.AddRange(existing, toUpdate);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = toUpdate.Id, Name = "Kitchen", Type = "Gym" });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.DuplicateName);
    }

    [Test]
    public async Task ShouldHaveSelfReferenceErrorWhenParentLocationIdEqualsOwnId()
    {
        await using var context = CreateContext();
        var location = new Location { Name = "Kitchen", Type = "Kitchen" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = location.Id, Name = "Kitchen", Type = "Kitchen", ParentLocationId = location.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.SelfReference);
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenParentLocationIdDoesNotExist()
    {
        await using var context = CreateContext();
        var location = new Location { Name = "Kitchen", Type = "Kitchen" };
        context.Locations.Add(location);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = location.Id, Name = "Kitchen", Type = "Kitchen", ParentLocationId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference);
    }

    [Test]
    public async Task ShouldHaveCircularReferenceErrorWhenParentIsADescendant()
    {
        // Hierarchy: A -> B -> C. Attempting to set A's parent to C (its own grandchild) is circular.
        await using var context = CreateContext();
        var a = new Location { Name = "A", Type = "Building" };
        context.Locations.Add(a);
        await context.SaveChangesAsync(CancellationToken.None);

        var b = new Location { Name = "B", Type = "Room", ParentLocationId = a.Id };
        context.Locations.Add(b);
        await context.SaveChangesAsync(CancellationToken.None);

        var c = new Location { Name = "C", Type = "Shelf", ParentLocationId = b.Id };
        context.Locations.Add(c);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = a.Id, Name = "A", Type = "Building", ParentLocationId = c.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.CircularReference);
    }

    [Test]
    public async Task ShouldNotHaveCircularReferenceErrorWhenParentIsUnrelated()
    {
        await using var context = CreateContext();
        var a = new Location { Name = "A", Type = "Building" };
        var unrelated = new Location { Name = "Unrelated", Type = "Building" };
        context.Locations.AddRange(a, unrelated);
        await context.SaveChangesAsync(CancellationToken.None);

        var validator = new UpdateLocationCommandValidator(context);
        var result = await validator.ValidateAsync(new UpdateLocationCommand { Id = a.Id, Name = "A", Type = "Building", ParentLocationId = unrelated.Id });

        result.Errors.ShouldNotContain(e => e.ErrorCode == ValidationErrorCodes.CircularReference);
    }
}
