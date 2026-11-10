using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationSplit;
using skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;
using skestock.Application.Features.SchoolClasses.Commands.InitializeSchoolClassConfiguration;
using skestock.Application.Features.SchoolClasses.Commands.UpdateClassDepartmentResponsiblePerson;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Users;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public sealed class ClassConfigurationTests : TestBase
{
    [Test]
    public async Task CreateClass_CopiesConfigurationAndKeepsSnapshotWhenGlobalConfigurationChanges()
    {
        await SetUpAdministratorAsync();
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 24 });
        await TestApp.SendAsync(Split(10, 5, 2, 3, 4));
        var initial = await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Name = "Bibliotecă", Responsibilities = "Organizează împrumutul de cărți"
        });

        var firstClass = await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = "Snapshot test - prima clasă",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 6, 1)
        });
        firstClass.Value.InvitationCount.ShouldBe(24);
        firstClass.Value.StudentInvitationCount.ShouldBe(10);
        firstClass.Value.ChangeInvitationCount.ShouldBe(4);
        firstClass.Value.Departments.Single().Name.ShouldBe("Bibliotecă");

        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 30 });
        await TestApp.SendAsync(Split(20, 6, 1, 0, 9));
        await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Id = initial.Value.Id, Name = "Bibliotecă nouă", Responsibilities = "Responsabilități noi"
        });
        var secondClass = await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = "Snapshot test - a doua clasă",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 6, 1)
        });

        var firstReloaded = await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = firstClass.Value.Id });
        firstReloaded.Value.InvitationCount.ShouldBe(24);
        firstReloaded.Value.StudentInvitationCount.ShouldBe(10);
        firstReloaded.Value.InstructorInvitationCount.ShouldBe(5);
        firstReloaded.Value.OpeningPrayerInvitationCount.ShouldBe(2);
        firstReloaded.Value.ExperienceInvitationCount.ShouldBe(3);
        firstReloaded.Value.ChangeInvitationCount.ShouldBe(4);
        firstReloaded.Value.Departments.Single().Name.ShouldBe("Bibliotecă");
        secondClass.Value.InvitationCount.ShouldBe(30);
        secondClass.Value.StudentInvitationCount.ShouldBe(20);
        secondClass.Value.ChangeInvitationCount.ShouldBe(9);
        secondClass.Value.Departments.Single().Name.ShouldBe("Bibliotecă nouă");
    }

    [Test]
    public async Task InitializeExistingClass_CopiesCurrentConfigurationOnlyOnce()
    {
        await SetUpAdministratorAsync();
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 18 });
        await TestApp.SendAsync(Split(7, 6, 5, 4, 3));
        await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Name = "Comunicare", Responsibilities = "Pregătește informările"
        });
        var schoolClass = new SchoolClass
        {
            Name = "Legacy class configuration test",
            StartDate = new DateOnly(2025, 9, 1),
            EndDate = new DateOnly(2026, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);

        var initialized = await TestApp.SendAsync(new InitializeSchoolClassConfigurationCommand
        {
            SchoolClassId = schoolClass.Id
        });
        initialized.IsSuccess.ShouldBeTrue();
        var classDetails = await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = schoolClass.Id });
        classDetails.Value.InvitationCount.ShouldBe(18);
        classDetails.Value.StudentInvitationCount.ShouldBe(7);
        classDetails.Value.InstructorInvitationCount.ShouldBe(6);
        classDetails.Value.OpeningPrayerInvitationCount.ShouldBe(5);
        classDetails.Value.ExperienceInvitationCount.ShouldBe(4);
        classDetails.Value.ChangeInvitationCount.ShouldBe(3);
        classDetails.Value.Departments.Single().ResponsiblePerson.ShouldBeNull();

        var repeated = await TestApp.SendAsync(new InitializeSchoolClassConfigurationCommand
        {
            SchoolClassId = schoolClass.Id
        });
        repeated.IsFailed.ShouldBeTrue();
        repeated.Errors.ShouldContain(error => error.Message.Contains("already has its initial configuration"));

        await TestApp.SendAsync(Split(1, 1, 1, 1, 1));
        (await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = schoolClass.Id }))
            .Value.StudentInvitationCount.ShouldBe(7);
    }

    private static SaveInvitationSplitCommand Split(int student, int instructor, int prayer, int experience,
        int change) => new()
    {
        StudentInvitationCount = student,
        InstructorInvitationCount = instructor,
        OpeningPrayerInvitationCount = prayer,
        ExperienceInvitationCount = experience,
        ChangeInvitationCount = change
    };

    // Every guarded command holds a shared class lock, and this one then updates the class row itself:
    // concurrent calls must end in one success and clean "already initialized" failures, never a deadlock error.
    [Test]
    public async Task InitializeExistingClass_WhenCalledConcurrently_InitializesOnceWithoutDeadlocking()
    {
        await SetUpAdministratorAsync();
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 18 });
        await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Comunicare", Responsibilities = "Informări" });
        var schoolClass = new SchoolClass
        {
            Name = "Concurrent configuration test",
            StartDate = new DateOnly(2025, 9, 1),
            EndDate = new DateOnly(2026, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);

        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ =>
            TestApp.SendAsync(new InitializeSchoolClassConfigurationCommand { SchoolClassId = schoolClass.Id })));

        results.Count(r => r.IsSuccess).ShouldBe(1);
        results.Where(r => r.IsFailed).ShouldAllBe(r =>
            r.Errors.Any(e => e.Message.Contains("already has its initial configuration")));
        var details = await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = schoolClass.Id });
        details.Value.Departments.Count.ShouldBe(1);
    }

    [Test]
    public async Task UpdateResponsiblePerson_ChangesOnlyTheSelectedClassDepartment()
    {
        await SetUpAdministratorAsync();
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 8 });
        await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Name = "Logistică", Responsibilities = "Gestionează materialele"
        });
        var schoolClass = await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = "Department responsible test",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 6, 1)
        });
        var department = schoolClass.Value.Departments.Single();

        var updated = await TestApp.SendAsync(new UpdateClassDepartmentResponsiblePersonCommand
        {
            SchoolClassId = schoolClass.Value.Id,
            DepartmentId = department.Id,
            ResponsiblePerson = "  Alex Popescu  "
        });
        updated.IsSuccess.ShouldBeTrue();

        var refreshed = await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = schoolClass.Value.Id });
        refreshed.Value.Departments.Single().ResponsiblePerson.ShouldBe("Alex Popescu");
        refreshed.Value.Departments.Single().Name.ShouldBe("Logistică");
        refreshed.Value.Departments.Single().Responsibilities.ShouldBe("Gestionează materialele");
    }

    private static async Task SetUpAdministratorAsync()
    {
        var identityId = await TestApp.RunAsAdministratorAsync();
        await TestApp.AddAsync(new UserProfile
        {
            IdentityId = identityId!.Value, FirstName = "Test", LastName = "Administrator"
        });
    }
}
