using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationSplit;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.UnitTests.Features.ClassConfiguration;

public class InvitationSplitCommandTests
{
    private static SaveInvitationSplitCommand Command(int student, int instructor, int prayer, int experience,
        int change) => new()
    {
        StudentInvitationCount = student,
        InstructorInvitationCount = instructor,
        OpeningPrayerInvitationCount = prayer,
        ExperienceInvitationCount = experience,
        ChangeInvitationCount = change
    };

    [Test]
    public async Task Save_CreatesConfigurationWithAllCounts()
    {
        await using var context = new ClassConfigurationTestDbContext();

        var result = await new SaveInvitationSplitCommandHandler(context.ApplicationContext)
            .Handle(Command(10, 5, 2, 3, 4), CancellationToken.None);

        result.Value.ShouldBe(new InvitationSplitDto(10, 5, 2, 3, 4));
        var configuration = await context.Configurations.SingleAsync();
        configuration.Id.ShouldBe(SharedClassConfiguration.SingletonId);
        configuration.StudentInvitationCount.ShouldBe(10);
        configuration.InstructorInvitationCount.ShouldBe(5);
        configuration.OpeningPrayerInvitationCount.ShouldBe(2);
        configuration.ExperienceInvitationCount.ShouldBe(3);
        configuration.ChangeInvitationCount.ShouldBe(4);
    }

    [Test]
    public async Task Save_UpdatesOnlySplitAndPreservesOtherConfiguration()
    {
        await using var context = new ClassConfigurationTestDbContext();
        var department = new DepartmentTemplate { Name = "Logistics", Responsibilities = "Stock" };
        context.Configurations.Add(new SharedClassConfiguration
        {
            InvitationCount = 42,
            Room4SeatCount = 10,
            Room2SeatCount = 20,
            Room6SeatCount = 30,
            StudentInvitationCount = 1,
            DepartmentTemplates = [department]
        });
        await context.SaveChangesAsync();

        var result = await new SaveInvitationSplitCommandHandler(context.ApplicationContext)
            .Handle(Command(20, 6, 1, 0, 200), CancellationToken.None);

        result.Value.ShouldBe(new InvitationSplitDto(20, 6, 1, 0, 200));
        var configuration = context.Configurations.Single();
        configuration.StudentInvitationCount.ShouldBe(20);
        configuration.InvitationCount.ShouldBe(42);
        configuration.Room4SeatCount.ShouldBe(10);
        configuration.Room2SeatCount.ShouldBe(20);
        configuration.Room6SeatCount.ShouldBe(30);
        configuration.DepartmentTemplates.Single().Id.ShouldBe(department.Id);
    }
}
