using System.Net;
using System.Net.Http.Json;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartments;
using skestock.Application.Features.ClassConfiguration.Queries.GetInvitationCount;
using skestock.Application.Common.Errors;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public class SharedClassConfigurationTests : TestBase
{
    [Test]
    public async Task WholeConfigurationEndpoints_AreRemoved()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();
        (await client.GetAsync("/api/ClassConfiguration")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/api/ClassConfiguration", new { invitationCount = 1 })).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Delete_DeletesOnlySelectedDepartmentAndRefreshesCache()
    {
        await TestApp.RunAsAdministratorAsync();
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 35 });
        var first = await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        var second = await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Events", Responsibilities = "Activities" });
        (await TestApp.SendAsync(new GetDepartmentsQuery())).Value.Count.ShouldBe(2);

        var deleted = await TestApp.SendAsync(new DeleteDepartmentCommand { Id = first.Value.Id });
        deleted.IsSuccess.ShouldBeTrue();
        (await TestApp.SendAsync(new GetDepartmentsQuery())).Value.Single().Id.ShouldBe(second.Value.Id);
        (await TestApp.SendAsync(new GetInvitationCountQuery())).Value.InvitationCount.ShouldBe(35);
        (await TestApp.FindAsync<DepartmentTemplate>(first.Value.Id)).ShouldBeNull();
    }

    [Test]
    public async Task Delete_LastDepartmentKeepsConfigurationAndInvitations()
    {
        await TestApp.RunAsAdministratorAsync();
        var saved = await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        await TestApp.SendAsync(new DeleteDepartmentCommand { Id = saved.Value.Id });
        (await TestApp.SendAsync(new GetDepartmentsQuery())).Value.ShouldBeEmpty();
        (await TestApp.FindAsync<SharedClassConfiguration>(SharedClassConfiguration.SingletonId)).ShouldNotBeNull();
    }

    [Test]
    public async Task Delete_UnknownIdReturnsNotFound()
    {
        await TestApp.RunAsAdministratorAsync();
        var result = await TestApp.SendAsync(new DeleteDepartmentCommand { Id = Guid.NewGuid() });
        result.Errors.ShouldContain(error => error is ClassConfigurationErrors.DepartmentNotFound);
    }

    [Test]
    public async Task Delete_AsNonAdministratorIsForbidden()
    {
        await TestApp.RunAsDefaultUserAsync();
        await Should.ThrowAsync<ForbiddenAccessException>(async () => await TestApp.SendAsync(new DeleteDepartmentCommand { Id = Guid.NewGuid() }));
    }

    [Test]
    public async Task DeleteEndpoint_AnonymousIsRejected()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();
        (await client.DeleteAsync($"/api/ClassConfiguration/departments/{Guid.NewGuid()}"))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteEndpoint_AdministratorCanDelete()
    {
        await TestApp.RunAsAdministratorAsync();
        var saved = await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        using var client = FunctionalTestSetup.Factory.CreateClient();
        (await client.PostAsJsonAsync("/api/Users/login?useCookies=true", new { email = "administrator@local", password = "Administrator1234!" }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.DeleteAsync($"/api/ClassConfiguration/departments/{saved.Value.Id}"))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await TestApp.FindAsync<DepartmentTemplate>(saved.Value.Id)).ShouldBeNull();
    }
}
