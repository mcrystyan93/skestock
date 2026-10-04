using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.ClassConfiguration.Queries.GetRoomConfiguration;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class RoomConfigurationQueryTests
{
    [Test]
    public async Task Get_WhenConfigurationIsMissing_ReturnsZeroCountsWithoutCreatingConfiguration()
    {
        await using var context = new ClassConfigurationTestDbContext();

        var result = await new GetRoomConfigurationQueryHandler(context.ApplicationContext)
            .Handle(new GetRoomConfigurationQuery(), CancellationToken.None);

        result.Value.Room4SeatCount.ShouldBe(0);
        result.Value.Room2SeatCount.ShouldBe(0);
        result.Value.Room6SeatCount.ShouldBe(0);
        context.Configurations.ShouldBeEmpty();
    }

    [Test]
    public async Task Get_ReturnsConfiguredSeatCounts()
    {
        await using var context = new ClassConfigurationTestDbContext();
        context.Configurations.Add(new SharedClassConfiguration
        {
            Room4SeatCount = 120,
            Room2SeatCount = 30,
            Room6SeatCount = 40
        });
        await context.SaveChangesAsync();

        var result = await new GetRoomConfigurationQueryHandler(context.ApplicationContext)
            .Handle(new GetRoomConfigurationQuery(), CancellationToken.None);

        result.Value.Room4SeatCount.ShouldBe(120);
        result.Value.Room2SeatCount.ShouldBe(30);
        result.Value.Room6SeatCount.ShouldBe(40);
    }
}
