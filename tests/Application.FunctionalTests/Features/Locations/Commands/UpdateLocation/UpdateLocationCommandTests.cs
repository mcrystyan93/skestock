using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Locations.Commands.CreateLocation;
using skestock.Application.Features.Locations.Commands.UpdateLocation;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithValidChanges_UpdatesLocationAndReturnsDto()
    {
        var created = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-Old", Type = "Kitchen" });

        var result = await TestApp.SendAsync(new UpdateLocationCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-New",
            Type = "StorageRoom"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe($"{_prefix}-New");
        result.Value.Type.ShouldBe("StorageRoom");

        var persisted = await TestApp.FindAsync<Location>(created.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe($"{_prefix}-New");
        persisted.Type.ShouldBe("StorageRoom");
    }

    [Test]
    public async Task Handle_WithParentLocationId_UpdatesRelationshipAndReturnsParentName()
    {
        var parent = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-Parent", Type = "Building" });
        var child = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-Child", Type = "Room" });

        var result = await TestApp.SendAsync(new UpdateLocationCommand
        {
            Id = child.Value.Id,
            Name = $"{_prefix}-Child",
            Type = "Room",
            ParentLocationId = parent.Value.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ParentLocationId.ShouldBe(parent.Value.Id);
        result.Value.ParentLocationName.ShouldBe($"{_prefix}-Parent");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new UpdateLocationCommand { Id = int.MaxValue, Name = $"{_prefix}-X", Type = "Room" });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithSelfAsParent_ThrowsValidationException()
    {
        var created = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-Self", Type = "Room" });

        var act = async () => await TestApp.SendAsync(new UpdateLocationCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Self",
            Type = "Room",
            ParentLocationId = created.Value.Id
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(UpdateLocationCommand.ParentLocationId));
    }

    [Test]
    public async Task Handle_WithCircularHierarchy_ThrowsValidationException()
    {
        var a = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-A", Type = "Building" });
        var b = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-B", Type = "Room", ParentLocationId = a.Value.Id });

        var act = async () => await TestApp.SendAsync(new UpdateLocationCommand
        {
            Id = a.Value.Id,
            Name = $"{_prefix}-A",
            Type = "Building",
            ParentLocationId = b.Value.Id
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(UpdateLocationCommand.ParentLocationId));
    }

    [Test]
    public async Task Handle_WithDuplicateNameOfAnotherLocation_ThrowsValidationException()
    {
        var first = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-First", Type = "Room" });
        var second = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-Second", Type = "Room" });

        var act = async () => await TestApp.SendAsync(new UpdateLocationCommand
        {
            Id = second.Value.Id,
            Name = $"{_prefix}-First",
            Type = "Room"
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(UpdateLocationCommand.Name));
    }

    [Test]
    public async Task Handle_WithSameNameOnSameLocation_DoesNotThrow()
    {
        var created = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-Same", Type = "Room" });

        var result = await TestApp.SendAsync(new UpdateLocationCommand
        {
            Id = created.Value.Id,
            Name = $"{_prefix}-Same",
            Type = "StorageRoom"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Type.ShouldBe("StorageRoom");
    }
}
