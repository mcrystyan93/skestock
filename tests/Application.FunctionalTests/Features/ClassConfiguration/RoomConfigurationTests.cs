using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.ClassConfiguration;
using skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Application.Features.ClassConfiguration.Queries.GetRoomConfiguration;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public class RoomConfigurationTests : TestBase
{
    [SetUp]
    public async Task ClearConfigurationCache()
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<HybridCache>()
            .RemoveByTagAsync(CacheConstants.ConfigurationTag);
    }

    [Test]
    public async Task Query_WhenConfigurationIsMissing_ReturnsZeroCountsWithoutCreatingSingleton()
    {
        await TestApp.RunAsDefaultUserAsync();

        var result = await TestApp.SendAsync(new GetRoomConfigurationQuery());

        result.Value.Room4SeatCount.ShouldBe(0);
        result.Value.Room2SeatCount.ShouldBe(0);
        result.Value.Room6SeatCount.ShouldBe(0);
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(0);
    }

    [Test]
    public async Task Query_PreservesPersistedSeatCountsAboveTheNewMaximum()
    {
        await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new SharedClassConfiguration
        {
            Room4SeatCount = 250,
            Room2SeatCount = 30,
            Room6SeatCount = 40
        });

        (await TestApp.SendAsync(new GetRoomConfigurationQuery())).Value
            .ShouldBe(new RoomConfigurationDto(250, 30, 40));
        (await TestApp.FindAsync<SharedClassConfiguration>(SharedClassConfiguration.SingletonId))!
            .Room4SeatCount.ShouldBe(250);
    }

    [Test]
    public async Task Save_CreatesAndUpdatesSingletonAndRefreshesCachedQuery()
    {
        await TestApp.RunAsAdministratorAsync();
        var query = new GetRoomConfigurationQuery();
        (await TestApp.SendAsync(query)).Value.ShouldBe(new RoomConfigurationDto(0, 0, 0));

        var created = await TestApp.SendAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = 120, Room2SeatCount = 30, Room6SeatCount = 40
        });
        (await TestApp.SendAsync(query)).Value.ShouldBe(new RoomConfigurationDto(120, 30, 40));

        var updated = await TestApp.SendAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = 135, Room2SeatCount = 32, Room6SeatCount = 45
        });
        (await TestApp.SendAsync(query)).Value.ShouldBe(new RoomConfigurationDto(135, 32, 45));
        updated.Value.ShouldBe(new RoomConfigurationDto(135, 32, 45));
        created.Value.ShouldBe(new RoomConfigurationDto(120, 30, 40));
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(1);
    }

    [Test]
    public async Task SavingInvitationsAndDepartments_PreservesRoomCounts()
    {
        await TestApp.RunAsAdministratorAsync();
        await TestApp.SendAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = 120, Room2SeatCount = 30, Room6SeatCount = 40
        });

        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 24 });
        var department = await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Name = "Logistics", Responsibilities = "Stock"
        });
        await TestApp.SendAsync(new DeleteDepartmentCommand { Id = department.Value.Id });

        (await TestApp.SendAsync(new GetRoomConfigurationQuery())).Value
            .ShouldBe(new RoomConfigurationDto(120, 30, 40));
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(1);
    }

    [Test]
    public async Task SaveCommand_RequiresAdministrator()
    {
        await TestApp.RunAsDefaultUserAsync();

        await Should.ThrowAsync<ForbiddenAccessException>(async () => await TestApp.SendAsync(
            new SaveRoomConfigurationCommand { Room4SeatCount = 1, Room2SeatCount = 1, Room6SeatCount = 1 }));
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(0);
    }
}
