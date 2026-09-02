using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Locations.Commands.CreateLocation;
using skestock.Application.Features.Locations.Queries.GetAllLocations;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Locations.Commands.CreateLocation;

public class CreateLocationCommandTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithValidNameAndType_PersistsLocationAndReturnsDto()
    {
        var name = $"{_prefix}-Kitchen";

        var result = await TestApp.SendAsync(new CreateLocationCommand { Name = name, Type = "Kitchen" });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(name);
        result.Value.Type.ShouldBe("Kitchen");
        result.Value.Id.ShouldNotBe(Guid.Empty);

        var persisted = await TestApp.FindAsync<Location>(result.Value.Id);
        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe(name);
        persisted.Type.ShouldBe("Kitchen");
    }

    [Test]
    public async Task Handle_WithParentLocationId_PersistsRelationshipAndReturnsParentName()
    {
        var parentResult = await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-MainBuilding", Type = "Building" });

        var result = await TestApp.SendAsync(new CreateLocationCommand
        {
            Name = $"{_prefix}-Room101",
            Type = "Room",
            ParentLocationId = parentResult.Value.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ParentLocationId.ShouldBe(parentResult.Value.Id);
        result.Value.ParentLocationName.ShouldBe($"{_prefix}-MainBuilding");
    }

    [Test]
    public async Task Handle_WithDuplicateName_ThrowsValidationException()
    {
        var name = $"{_prefix}-Kitchen";
        await TestApp.SendAsync(new CreateLocationCommand { Name = name, Type = "Kitchen" });

        var act = async () => await TestApp.SendAsync(new CreateLocationCommand { Name = name, Type = "StorageRoom" });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateLocationCommand.Name));
    }

    [Test]
    public async Task Handle_WithEmptyName_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateLocationCommand { Name = "", Type = "Kitchen" });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateLocationCommand.Name));
    }

    [Test]
    public async Task Handle_WithEmptyType_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateLocationCommand { Name = $"{_prefix}-X", Type = "" });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateLocationCommand.Type));
    }

    [Test]
    public async Task Handle_WithNonExistentParentLocationId_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new CreateLocationCommand
        {
            Name = $"{_prefix}-Room",
            Type = "Room",
            ParentLocationId = Guid.NewGuid()
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(CreateLocationCommand.ParentLocationId));
    }

    [Test]
    public async Task Handle_OnSuccess_InvalidatesGetAllLocationsCacheForSameSearchTerm()
    {
        var before = await TestApp.SendAsync(new GetAllLocationsQuery { SearchTerm = _prefix });
        before.Value.Data.Count().ShouldBe(0);

        var name = $"{_prefix}-Electronics";
        await TestApp.SendAsync(new CreateLocationCommand { Name = name, Type = "StorageRoom" });

        var after = await TestApp.SendAsync(new GetAllLocationsQuery { SearchTerm = _prefix });
        after.Value.Data.Count().ShouldBe(1);
        after.Value.Data.Single().Name.ShouldBe(name);
    }
}
