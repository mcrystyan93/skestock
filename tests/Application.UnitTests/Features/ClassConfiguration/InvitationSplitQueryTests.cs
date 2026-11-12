using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Application.Features.ClassConfiguration.Queries.GetInvitationSplit;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class InvitationSplitQueryTests
{
    [Test]
    public async Task Get_WhenConfigurationIsMissing_ReturnsZeroCountsWithoutCreatingConfiguration()
    {
        await using var context = new ClassConfigurationTestDbContext();

        var result = await new GetInvitationSplitQueryHandler(context.ApplicationContext)
            .Handle(new GetInvitationSplitQuery(), CancellationToken.None);

        result.Value.ShouldBe(new InvitationSplitDto(0, 0, 0, 0, 0));
        context.Configurations.ShouldBeEmpty();
    }

    [Test]
    public async Task Get_ReturnsConfiguredCounts()
    {
        await using var context = new ClassConfigurationTestDbContext();
        context.Configurations.Add(new SharedClassConfiguration
        {
            StudentInvitationCount = 10,
            InstructorInvitationCount = 5,
            OpeningPrayerInvitationCount = 2,
            ExperienceInvitationCount = 3,
            ChangeInvitationCount = 4
        });
        await context.SaveChangesAsync();

        var result = await new GetInvitationSplitQueryHandler(context.ApplicationContext)
            .Handle(new GetInvitationSplitQuery(), CancellationToken.None);

        result.Value.ShouldBe(new InvitationSplitDto(10, 5, 2, 3, 4));
    }
}
