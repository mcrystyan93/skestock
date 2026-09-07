using skestock.Application.Features.Locations.Queries.GetDefaultLocation;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Locations.Queries.GetDefaultLocation;

public class GetDefaultLocationQueryTests : TestBase
{
    private string _prefix = null!;

    [SetUp]
    public void SetUpPrefix()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];
    }

    [Test]
    public async Task Handle_WithDefaultLocation_ReturnsDefaultDto()
    {
        await TestApp.AddAsync(new Location { Name = $"{_prefix}-NonDefault", Type = "Bucatarie", IsDefault = false });
        var defaultLocation = new Location { Name = $"{_prefix}-Camara", Type = "Bucatarie", IsDefault = true };
        await TestApp.AddAsync(defaultLocation);

        var result = await TestApp.SendAsync(new GetDefaultLocationQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(defaultLocation.Id);
        result.Value.Name.ShouldBe(defaultLocation.Name);
        result.Value.IsDefault.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithNoDefaultLocation_ReturnsSuccessWithNull()
    {
        await TestApp.AddAsync(new Location { Name = $"{_prefix}-NonDefault", Type = "Bucatarie", IsDefault = false });

        var result = await TestApp.SendAsync(new GetDefaultLocationQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }
}
