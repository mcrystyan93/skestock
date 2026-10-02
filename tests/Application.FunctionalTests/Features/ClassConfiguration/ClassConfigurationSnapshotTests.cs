using skestock.Application.Features.ClassConfiguration.Commands.SaveSharedClassConfiguration;
using skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;
using skestock.Application.Features.SchoolClasses.Commands.InitializeSchoolClassConfiguration;
using skestock.Application.Features.SchoolClasses.Commands.UpdateClassDepartmentResponsiblePerson;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public sealed class ClassConfigurationTests : TestBase
{
    [Test]
    public async Task CreateClass_CopiesConfigurationAndKeepsSnapshotWhenGlobalConfigurationChanges()
    {
        await SetUpAdministratorAsync();
        var initial = await TestApp.SendAsync(new SaveSharedClassConfigurationCommand
        {
            InvitationCount = 24,
            Departments = [new DepartmentTemplateInput(null, "Bibliotecă", "Organizează împrumutul de cărți")]
        });

        var firstClass = await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = "Snapshot test - prima clasă",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 6, 1)
        });
        firstClass.Value.InvitationCount.ShouldBe(24);
        firstClass.Value.Departments.Single().Name.ShouldBe("Bibliotecă");

        await TestApp.SendAsync(new SaveSharedClassConfigurationCommand
        {
            InvitationCount = 30,
            Departments =
            [
                new DepartmentTemplateInput(initial.Value.Departments.Single().Id, "Bibliotecă nouă",
                    "Responsabilități noi")
            ]
        });
        var secondClass = await TestApp.SendAsync(new CreateSchoolClassCommand
        {
            Name = "Snapshot test - a doua clasă",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 6, 1)
        });

        var firstReloaded = await TestApp.SendAsync(new GetSchoolClassByIdQuery { Id = firstClass.Value.Id });
        firstReloaded.Value.InvitationCount.ShouldBe(24);
        firstReloaded.Value.Departments.Single().Name.ShouldBe("Bibliotecă");
        secondClass.Value.InvitationCount.ShouldBe(30);
        secondClass.Value.Departments.Single().Name.ShouldBe("Bibliotecă nouă");
    }

    [Test]
    public async Task InitializeExistingClass_CopiesCurrentConfigurationOnlyOnce()
    {
        await SetUpAdministratorAsync();
        await TestApp.SendAsync(new SaveSharedClassConfigurationCommand
        {
            InvitationCount = 18,
            Departments = [new DepartmentTemplateInput(null, "Comunicare", "Pregătește informările")]
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
        classDetails.Value.Departments.Single().ResponsiblePerson.ShouldBeNull();

        var repeated = await TestApp.SendAsync(new InitializeSchoolClassConfigurationCommand
        {
            SchoolClassId = schoolClass.Id
        });
        repeated.IsFailed.ShouldBeTrue();
        repeated.Errors.ShouldContain(error => error.Message.Contains("already has its initial configuration"));
    }

    [Test]
    public async Task UpdateResponsiblePerson_ChangesOnlyTheSelectedClassDepartment()
    {
        await SetUpAdministratorAsync();
        await TestApp.SendAsync(new SaveSharedClassConfigurationCommand
        {
            InvitationCount = 8,
            Departments = [new DepartmentTemplateInput(null, "Logistică", "Gestionează materialele")]
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
