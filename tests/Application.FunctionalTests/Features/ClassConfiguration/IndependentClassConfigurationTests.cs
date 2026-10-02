using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Common.Exceptions;
using skestock.Application.Features.ClassConfiguration;
using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartments;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;
using skestock.Application.Features.ClassConfiguration.Queries.GetInvitationCount;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public class IndependentClassConfigurationTests : TestBase
{
    [SetUp]
    public async Task ClearConfigurationCache()
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<HybridCache>()
            .RemoveByTagAsync(CacheConstants.ConfigurationTag);
    }

    [Test]
    public async Task Queries_WithNoConfiguration_ReturnEmptyDefaultsWithoutCreatingConfiguration()
    {
        await TestApp.RunAsDefaultUserAsync();
        var departments = await TestApp.SendAsync(new GetDepartmentsQuery());
        var invitations = await TestApp.SendAsync(new GetInvitationCountQuery());

        departments.IsSuccess.ShouldBeTrue();
        departments.Value.ShouldBeEmpty();
        invitations.Value.InvitationCount.ShouldBe(0);
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(0);
    }

    [Test]
    public async Task SaveDepartment_CreatesUpdatesAndPreservesOtherConfiguration()
    {
        await TestApp.RunAsAdministratorAsync();
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 42 });
        var first = await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Name = " Logistics ", Responsibilities = " Stock "
        });
        var other = await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Name = "Events", Responsibilities = "Activities"
        });
        var before = await TestApp.SendAsync(new GetDepartmentsQuery());
        before.Value.Count.ShouldBe(2);
        var update = await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Id = first.Value.Id, Name = " Purchasing ", Responsibilities = " Orders "
        });
        var after = await TestApp.SendAsync(new GetDepartmentsQuery());
        var invitations = await TestApp.SendAsync(new GetInvitationCountQuery());

        first.Value.Id.ShouldNotBe(Guid.Empty);
        first.Value.Name.ShouldBe("Logistics");
        update.Value.Id.ShouldBe(first.Value.Id);
        update.Value.Responsibilities.ShouldBe("Orders");
        after.Value.Select(department => department.Name).ShouldBe(["Events", "Purchasing"]);
        after.Value.ShouldContain(department =>
            department.Id == other.Value.Id && department.Responsibilities == "Activities");
        invitations.Value.InvitationCount.ShouldBe(42);
    }

    [Test]
    public async Task SaveDepartment_AsFirstSave_CreatesConfigurationWithZeroInvitations()
    {
        await TestApp.RunAsAdministratorAsync();
        await TestApp.SendAsync(new GetDepartmentsQuery());
        var created =
            await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        var departments = await TestApp.SendAsync(new GetDepartmentsQuery());

        departments.Value.Single().Id.ShouldBe(created.Value.Id);
        (await TestApp.FindAsync<SharedClassConfiguration>(SharedClassConfiguration.SingletonId))!
            .InvitationCount.ShouldBe(0);
    }

    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public async Task SaveInvitations_AsFirstSave_PersistsExplicitConfiguration(int count)
    {
        await TestApp.RunAsAdministratorAsync();
        await TestApp.SendAsync(new GetInvitationCountQuery());
        var saved = await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = count });
        var read = await TestApp.SendAsync(new GetInvitationCountQuery());

        saved.Value.InvitationCount.ShouldBe(count);
        read.Value.InvitationCount.ShouldBe(count);
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(1);
    }

    [Test]
    public async Task SaveInvitations_PreservesDepartmentsAndRefreshesCachedCount()
    {
        await TestApp.RunAsAdministratorAsync();
        var department =
            await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        await TestApp.SendAsync(new GetDepartmentsQuery());
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 10 });
        (await TestApp.SendAsync(new GetInvitationCountQuery())).Value.InvitationCount.ShouldBe(10);
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 20 });

        (await TestApp.SendAsync(new GetInvitationCountQuery())).Value.InvitationCount.ShouldBe(20);
        (await TestApp.SendAsync(new GetDepartmentsQuery())).Value.Single().Id.ShouldBe(department.Value.Id);
    }

    [Test]
    public async Task IndependentSaves_InvalidateNewQueries()
    {
        await TestApp.RunAsAdministratorAsync();
        await TestApp.SendAsync(new GetDepartmentsQuery());
        await TestApp.SendAsync(new GetInvitationCountQuery());
        await TestApp.SendAsync(new SaveInvitationCountCommand { InvitationCount = 12 });
        await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });

        (await TestApp.SendAsync(new GetDepartmentsQuery())).Value.Single().Name.ShouldBe("Logistics");
        (await TestApp.SendAsync(new GetInvitationCountQuery())).Value.InvitationCount.ShouldBe(12);
    }

    [Test]
    public async Task SaveDepartment_RejectsDuplicateNameAndAllowsOwnName()
    {
        await TestApp.RunAsAdministratorAsync();
        var first = await TestApp.SendAsync(
            new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        await Should.ThrowAsync<ValidationException>(async () => await TestApp.SendAsync(
            new SaveDepartmentCommand { Name = " LOGISTICS ", Responsibilities = "Work" }));
        var update = await TestApp.SendAsync(new SaveDepartmentCommand
        {
            Id = first.Value.Id, Name = " LOGISTICS ", Responsibilities = "Orders"
        });
        update.IsSuccess.ShouldBeTrue();
        (await TestApp.CountAsync<DepartmentTemplate>()).ShouldBe(1);
    }

    [Test]
    public async Task SaveInvitations_NegativeCountIsRejectedWithoutCreatingConfiguration()
    {
        await TestApp.RunAsAdministratorAsync();
        await Should.ThrowAsync<ValidationException>(async () => await TestApp.SendAsync(
            new SaveInvitationCountCommand { InvitationCount = -1 }));
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(0);
    }

    [Test]
    public async Task Commands_RequireAdministrator()
    {
        await TestApp.RunAsDefaultUserAsync();
        await Should.ThrowAsync<ForbiddenAccessException>(async () => await TestApp.SendAsync(
            new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" }));
        await Should.ThrowAsync<ForbiddenAccessException>(async () => await TestApp.SendAsync(
            new SaveInvitationCountCommand { InvitationCount = 12 }));
    }

    [TestCase("departments")]
    [TestCase("invitations")]
    public async Task Endpoints_RejectAnonymousRequests(string resource)
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();
        (await client.GetAsync($"/api/ClassConfiguration/{resource}")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync($"/api/ClassConfiguration/{resource}",
                new { name = "Logistics", responsibilities = "Stock", invitationCount = 12 }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [TestCase("departments")]
    [TestCase("invitations")]
    public async Task Endpoints_AllowReadsButForbidSavesForNonAdministrator(string resource)
    {
        using var client = await LoginAsync(false);
        (await client.GetAsync($"/api/ClassConfiguration/{resource}")).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PutAsJsonAsync($"/api/ClassConfiguration/{resource}",
                new { name = "Logistics", responsibilities = "Stock", invitationCount = 12 }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Endpoints_AdministratorCanCreateUpdateAndReadBothResources()
    {
        using var client = await LoginAsync(true);
        var createdResponse = await client.PutAsJsonAsync("/api/ClassConfiguration/departments",
            new { name = "Logistics", responsibilities = "Stock" });
        createdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var created = (await createdResponse.Content.ReadFromJsonAsync<DepartmentTemplateDto>())!;
        var updatedResponse = await client.PutAsJsonAsync("/api/ClassConfiguration/departments",
            new { id = created.Id, name = "Purchasing", responsibilities = "Orders" });
        updatedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var invitationsResponse =
            await client.PutAsJsonAsync("/api/ClassConfiguration/invitations", new { invitationCount = 31 });
        invitationsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var departments = await client.GetFromJsonAsync<DepartmentTemplateDto[]>("/api/ClassConfiguration/departments");
        var invitations = await client.GetFromJsonAsync<InvitationCountDto>("/api/ClassConfiguration/invitations");
        departments!.Single().Id.ShouldBe(created.Id);
        departments!.Single().Name.ShouldBe("Purchasing");
        invitations!.InvitationCount.ShouldBe(31);
    }

    [Test]
    public async Task Endpoint_UnknownDepartmentReturns404WithoutWriting()
    {
        using var client = await LoginAsync(true);
        var response = await client.PutAsJsonAsync("/api/ClassConfiguration/departments",
            new { id = Guid.NewGuid(), name = "Missing", responsibilities = "Work" });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(0);
    }

    [TestCase("departments")]
    [TestCase("invitations")]
    public async Task Endpoints_ReturnValidationProblemForInvalidInput(string resource)
    {
        using var client = await LoginAsync(true);
        var response = await client.PutAsJsonAsync($"/api/ClassConfiguration/{resource}",
            new { name = " ", responsibilities = " ", invitationCount = -1 });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await TestApp.CountAsync<SharedClassConfiguration>()).ShouldBe(0);
    }


    [Test]
    public async Task GetDepartment_CacheIsRefreshedAfterSaveAndDelete()
    {
        await TestApp.RunAsAdministratorAsync();
        var saved = await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        var query = new GetDepartmentQuery { Id = saved.Value.Id };
        (await TestApp.SendAsync(query)).Value.Name.ShouldBe("Logistics");

        await TestApp.SendAsync(new SaveDepartmentCommand { Id = saved.Value.Id, Name = "Purchasing", Responsibilities = "Orders" });
        (await TestApp.SendAsync(query)).Value.Name.ShouldBe("Purchasing");

        await TestApp.SendAsync(new DeleteDepartmentCommand { Id = saved.Value.Id });
        (await TestApp.SendAsync(query)).IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task GetDepartmentEndpoint_AuthenticatedUserCanReadAndUnknownReturns404()
    {
        await TestApp.RunAsAdministratorAsync();
        var saved = await TestApp.SendAsync(new SaveDepartmentCommand { Name = "Logistics", Responsibilities = "Stock" });
        using var client = await LoginAsync(false);
        var department = await client.GetFromJsonAsync<DepartmentTemplateDto>($"/api/ClassConfiguration/departments/{saved.Value.Id}");
        department!.Id.ShouldBe(saved.Value.Id);
        department.Name.ShouldBe("Logistics");
        (await client.GetAsync($"/api/ClassConfiguration/departments/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetDepartmentEndpoint_AnonymousIsRejected()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();
        (await client.GetAsync($"/api/ClassConfiguration/departments/{Guid.NewGuid()}")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetDepartmentEndpoint_EmptyIdReturnsValidationProblem()
    {
        using var client = await LoginAsync(false);
        var response = await client.GetAsync($"/api/ClassConfiguration/departments/{Guid.Empty}");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    private static async Task<HttpClient> LoginAsync(bool administrator)
    {
        if (administrator) await TestApp.RunAsAdministratorAsync();
        else await TestApp.RunAsDefaultUserAsync();
        var client = FunctionalTestSetup.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new
            {
                email = administrator ? "administrator@local" : "test@local",
                password = administrator ? "Administrator1234!" : "Testing1234!"
            });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }
}
