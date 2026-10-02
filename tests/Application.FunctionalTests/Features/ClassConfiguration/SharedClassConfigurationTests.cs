using System.Net.Http.Json;
using System.Net;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.ClassConfiguration.Commands.SaveSharedClassConfiguration;
using skestock.Application.Features.ClassConfiguration.Queries.GetSharedClassConfiguration;
using skestock.Application.FunctionalTests.Infrastructure;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public class SharedClassConfigurationTests : TestBase
{
    [Test]
    public async Task Get_WithNoSavedConfiguration_ReportsNotConfigured()
    {
        await TestApp.RunAsAdministratorAsync();

        var result = await TestApp.SendAsync(new GetSharedClassConfigurationQuery());

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsConfigured.ShouldBeFalse();
        result.Value.InvitationCount.ShouldBe(0);
    }

    [Test]
    public async Task Save_WithZeroInvitations_PersistsAnExplicitConfiguration()
    {
        await TestApp.RunAsAdministratorAsync();

        var save = await TestApp.SendAsync(new SaveSharedClassConfigurationCommand { InvitationCount = 0 });
        var read = await TestApp.SendAsync(new GetSharedClassConfigurationQuery());

        save.IsSuccess.ShouldBeTrue();
        read.IsSuccess.ShouldBeTrue();
        read.Value.IsConfigured.ShouldBeTrue();
        read.Value.InvitationCount.ShouldBe(0);
    }

    [Test]
    public async Task Save_WithDepartmentTemplates_CreatesUpdatesAndDeletesTemplates()
    {
        await TestApp.RunAsAdministratorAsync();

        var created = await TestApp.SendAsync(new SaveSharedClassConfigurationCommand
        {
            InvitationCount = 30,
            Departments =
            [
                new DepartmentTemplateInput(null, "Logistică", "Gestionează stocurile."),
                new DepartmentTemplateInput(null, "Comunicare", "Coordonează anunțurile.")
            ]
        });

        var logistics = created.Value.Departments.Single(department => department.Name == "Logistică");

        var updated = await TestApp.SendAsync(new SaveSharedClassConfigurationCommand
        {
            InvitationCount = 35,
            Departments =
            [
                new DepartmentTemplateInput(logistics.Id, "Aprovizionare", "Gestionează comenzile și stocurile."),
                new DepartmentTemplateInput(null, "Evenimente", "Organizează activitățile clasei.")
            ]
        });

        updated.Value.InvitationCount.ShouldBe(35);
        updated.Value.Departments.Count.ShouldBe(2);
        updated.Value.Departments.ShouldContain(department =>
            department.Id == logistics.Id &&
            department.Name == "Aprovizionare" &&
            department.Responsibilities == "Gestionează comenzile și stocurile.");

        var reloaded = await TestApp.SendAsync(new GetSharedClassConfigurationQuery());
        reloaded.Value.Departments.Select(department => department.Name)
            .ShouldBe(["Aprovizionare", "Evenimente"]);
    }

    [Test]
    public async Task Save_WithDuplicateDepartmentNames_ThrowsValidationException()
    {
        await TestApp.RunAsAdministratorAsync();

        var act = async () => await TestApp.SendAsync(new SaveSharedClassConfigurationCommand
        {
            InvitationCount = 0,
            Departments =
            [
                new DepartmentTemplateInput(null, "Comunicare", "Planifică anunțuri."),
                new DepartmentTemplateInput(null, " comunicare ", "Coordonează întâlniri.")
            ]
        });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(SaveSharedClassConfigurationCommand.Departments));
    }

    [Test]
    public async Task Save_WithNegativeInvitationCount_ThrowsValidationException()
    {
        await TestApp.RunAsAdministratorAsync();

        var act = async () => await TestApp.SendAsync(new SaveSharedClassConfigurationCommand { InvitationCount = -1 });

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(SaveSharedClassConfigurationCommand.InvitationCount));
    }

    [Test]
    public async Task Save_AsNonAdministrator_ThrowsForbiddenAccessException()
    {
        await TestApp.RunAsDefaultUserAsync();

        var act = async () => await TestApp.SendAsync(new SaveSharedClassConfigurationCommand { InvitationCount = 20 });

        await act.ShouldThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Save_Anonymous_IsRejected()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/ClassConfiguration",
            new { invitationCount = 20, departments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
