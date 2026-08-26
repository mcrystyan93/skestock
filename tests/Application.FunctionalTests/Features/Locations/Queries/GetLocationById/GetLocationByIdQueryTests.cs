using skestock.Application.Common.Exceptions;
using skestock.Application.Features.Locations.Queries.GetLocationById;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdQueryTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithExistingId_ReturnsMatchingLocationDto()
    {
        var location = new Location { Name = $"{_prefix}-Kitchen", Type = "Kitchen" };
        await TestApp.AddAsync(location);

        var result = await TestApp.SendAsync(new GetLocationByIdQuery { Id = location.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(location.Id);
        result.Value.Name.ShouldBe(location.Name);
        result.Value.Type.ShouldBe(location.Type);
    }

    [Test]
    public async Task Handle_WithParentLocation_ReturnsParentLocationNameInDto()
    {
        var parent = new Location { Name = $"{_prefix}-Parent", Type = "Building" };
        await TestApp.AddAsync(parent);
        var child = new Location { Name = $"{_prefix}-Child", Type = "Room", ParentLocationId = parent.Id };
        await TestApp.AddAsync(child);

        var result = await TestApp.SendAsync(new GetLocationByIdQuery { Id = child.Id });

        result.IsSuccess.ShouldBeTrue();
        result.Value.ParentLocationId.ShouldBe(parent.Id);
        result.Value.ParentLocationName.ShouldBe(parent.Name);
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var result = await TestApp.SendAsync(new GetLocationByIdQuery { Id = int.MaxValue });

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithInvalidId_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(new GetLocationByIdQuery { Id = 0 });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetLocationByIdQuery.Id));
    }
}
