using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class RoomConfigurationCommandTests
{
    [Test]
    public async Task Save_CreatesConfigurationWithAllSeatCounts()
    {
        await using var context = new ClassConfigurationTestDbContext();

        var result = await new SaveRoomConfigurationCommandHandler(context.ApplicationContext).Handle(
            new SaveRoomConfigurationCommand { Room4SeatCount = 120, Room2SeatCount = 30, Room6SeatCount = 40 },
            CancellationToken.None);

        result.Value.Room4SeatCount.ShouldBe(120);
        result.Value.Room2SeatCount.ShouldBe(30);
        result.Value.Room6SeatCount.ShouldBe(40);
        var configuration = await context.Configurations.Include(item => item.DepartmentTemplates).SingleAsync();
        configuration.Id.ShouldBe(SharedClassConfiguration.SingletonId);
        configuration.Room4SeatCount.ShouldBe(120);
        configuration.Room2SeatCount.ShouldBe(30);
        configuration.Room6SeatCount.ShouldBe(40);
    }

    [Test]
    public async Task Save_UpdatesOnlySeatCountsAndPreservesOtherConfiguration()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var department = new DepartmentTemplate { Name = "Logistics", Responsibilities = "Stock" };
        context.Configurations.Add(new SharedClassConfiguration
        {
            InvitationCount = 42,
            Room4SeatCount = 10,
            Room2SeatCount = 20,
            Room6SeatCount = 30,
            DepartmentTemplates = [department]
        });
        await context.SaveChangesAsync();

        var result = await new SaveRoomConfigurationCommandHandler(context.ApplicationContext).Handle(
            new SaveRoomConfigurationCommand { Room4SeatCount = 120, Room2SeatCount = 35, Room6SeatCount = 45 },
            CancellationToken.None);

        result.Value.Room4SeatCount.ShouldBe(120);
        result.Value.Room2SeatCount.ShouldBe(35);
        result.Value.Room6SeatCount.ShouldBe(45);
        var configuration = context.Configurations.Single();
        configuration.InvitationCount.ShouldBe(42);
        configuration.DepartmentTemplates.Single().Id.ShouldBe(department.Id);
    }
}
