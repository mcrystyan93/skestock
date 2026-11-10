using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Features.ClassConfiguration;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public class InvitationSplitEndpointTests : TestBase
{
    private const string Url = "/api/ClassConfiguration/invitation-split";

    private static object Payload(int student = 10, int instructor = 5, int prayer = 2, int experience = 3,
        int change = 4) => new
    {
        studentInvitationCount = student,
        instructorInvitationCount = instructor,
        openingPrayerInvitationCount = prayer,
        experienceInvitationCount = experience,
        changeInvitationCount = change
    };

    [SetUp]
    public async Task ClearConfigurationCache()
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<HybridCache>()
            .RemoveByTagAsync(CacheConstants.ConfigurationTag);
    }

    [Test]
    public async Task Endpoints_RejectAnonymousRequests()
    {
        using var client = FunctionalTestSetup.Factory.CreateClient();

        (await client.GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync(Url, Payload())).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Endpoints_AllowReadsAndForbidWritesForNonAdministrator()
    {
        using var client = await LoginAsync(administrator: false);

        var getResponse = await client.GetAsync(Url);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await getResponse.Content.ReadFromJsonAsync<InvitationSplitDto>())!
            .ShouldBe(new InvitationSplitDto(0, 0, 0, 0, 0));
        (await client.PutAsJsonAsync(Url, Payload())).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Administrator_CanCreateUpdateAndReadSplit()
    {
        using var client = await LoginAsync(administrator: true);

        var createResponse = await client.PutAsJsonAsync(Url, Payload());
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await createResponse.Content.ReadFromJsonAsync<InvitationSplitDto>())!
            .ShouldBe(new InvitationSplitDto(10, 5, 2, 3, 4));

        var updateResponse = await client.PutAsJsonAsync(Url, Payload(200, 0, 200, 0, 200));
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await client.GetFromJsonAsync<InvitationSplitDto>(Url))
            .ShouldBe(new InvitationSplitDto(200, 0, 200, 0, 200));
    }

    [Test]
    public async Task SaveEndpoint_InvalidPayloadReturnsValidationProblemWithoutWriting()
    {
        using var client = await LoginAsync(administrator: true);
        (await client.PutAsJsonAsync(Url, Payload())).StatusCode.ShouldBe(HttpStatusCode.OK);

        var incomplete = await client.PutAsJsonAsync(Url, new { studentInvitationCount = 1 });
        var negative = await client.PutAsJsonAsync(Url, Payload(instructor: -1));
        var tooLarge = await client.PutAsJsonAsync(Url, Payload(change: 201));
        var fractional = await client.PutAsync(Url, JsonContent.Create(new
        {
            studentInvitationCount = 1.5,
            instructorInvitationCount = 1,
            openingPrayerInvitationCount = 1,
            experienceInvitationCount = 1,
            changeInvitationCount = 1
        }));

        incomplete.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        incomplete.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        negative.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        tooLarge.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        fractional.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.GetFromJsonAsync<InvitationSplitDto>(Url)).ShouldBe(new InvitationSplitDto(10, 5, 2, 3, 4));
        (await TestApp.CountAsync<skestock.Domain.Entities.SchoolClasses.SharedClassConfiguration>()).ShouldBe(1);
    }

    private static async Task<HttpClient> LoginAsync(bool administrator)
    {
        if (administrator) await TestApp.RunAsAdministratorAsync();
        else await TestApp.RunAsDefaultUserAsync();

        var client = FunctionalTestSetup.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true", new
        {
            email = administrator ? "administrator@local" : "test@local",
            password = administrator ? "Administrator1234!" : "Testing1234!"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }
}
