using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.ClassConfiguration;
using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationSplit;
using skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Application.Features.ClassConfiguration.Queries.GetInvitationCount;
using skestock.Application.Features.ClassConfiguration.Queries.GetInvitationSplit;
using skestock.Application.Features.ClassConfiguration.Queries.GetRoomConfiguration;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public class InvitationSplitTests : TestBase
{
    private static SaveInvitationSplitCommand Split(int student, int instructor, int prayer, int experience,
        int change) => new()
    {
        StudentInvitationCount = student,
        InstructorInvitationCount = instructor,
        OpeningPrayerInvitationCount = prayer,
        ExperienceInvitationCount = experience,
        ChangeInvitationCount = change
    };

    [SetUp]
    public async Task ClearConfigurationCache()
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<HybridCache>()
            .RemoveByTagAsync(CacheConstants.ConfigurationTag);
    }

    [Test]
    public async Task Save_CreatesAndUpdatesSingletonAndRefreshesCachedQuery()
    {
        await TestApp.RunAsAdministratorAsync();
        var query = new GetInvitationSplitQuery();
        (await TestApp.SendAsync(query)).Value.ShouldBe(new InvitationSplitDto(0, 0, 0, 0, 0));

        await TestApp.SendAsync(Split(10, 5, 2, 3, 4));
        (await TestApp.SendAsync(query)).Value.ShouldBe(new InvitationSplitDto(10, 5, 2, 3, 4));

        await TestApp.SendAsync(Split(11, 6, 3, 4, 5));
        (await TestApp.SendAsync(query)).Value.ShouldBe(new InvitationSplitDto(11, 6, 3, 4, 5));
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(1);
    }

    [Test]
    public async Task SavingOtherSections_PreservesSplitAndViceVersa()
    {
        await TestApp.RunAsAdministratorAsync();
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 24 });
        await TestApp.SendAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = 120, Room2SeatCount = 30, Room6SeatCount = 40
        });
        await TestApp.SendAsync(Split(10, 5, 2, 3, 4));

        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 25 });
        await TestApp.SendAsync(new SaveRoomConfigurationCommand
        {
            Room4SeatCount = 121, Room2SeatCount = 31, Room6SeatCount = 41
        });
        await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });

        (await TestApp.SendAsync(new GetInvitationSplitQuery())).Value
            .ShouldBe(new InvitationSplitDto(10, 5, 2, 3, 4));

        await TestApp.SendAsync(Split(1, 1, 1, 1, 1));

        (await TestApp.SendAsync(new GetInvitationCountQuery())).Value.InvitationCount.ShouldBe(25);
        (await TestApp.SendAsync(new GetRoomConfigurationQuery())).Value
            .ShouldBe(new RoomConfigurationDto(121, 31, 41));
    }

    [Test]
    public async Task SaveCommand_RequiresAdministrator()
    {
        await TestApp.RunAsDefaultUserAsync();

        await Should.ThrowAsync<ForbiddenAccessException>(async () => await TestApp.SendAsync(Split(1, 1, 1, 1, 1)));
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(0);
    }
}
