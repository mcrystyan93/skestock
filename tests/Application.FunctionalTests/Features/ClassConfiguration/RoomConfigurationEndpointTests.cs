using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Features.ClassConfiguration;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.FunctionalTests.Features.ClassConfiguration;

public class RoomConfigurationEndpointTests : TestBase
{
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

        (await client.GetAsync("/api/ClassConfiguration/rooms")).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.PutAsJsonAsync("/api/ClassConfiguration/rooms",
            new { room4SeatCount = 120, room1SeatCount = 30, room6SeatCount = 40 }))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Endpoints_AllowReadsAndForbidWritesForNonAdministrator()
    {
        using var client = await LoginAsync(administrator: false);

        var getResponse = await client.GetAsync("/api/ClassConfiguration/rooms");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await getResponse.Content.ReadFromJsonAsync<RoomConfigurationDto>())!
            .ShouldBe(new RoomConfigurationDto(0, 0, 0));
        (await client.PutAsJsonAsync("/api/ClassConfiguration/rooms",
                new { room4SeatCount = 120, room1SeatCount = 30, room6SeatCount = 40 }))
            .StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Administrator_CanCreateUpdateAndReadRoomConfiguration()
    {
        using var client = await LoginAsync(administrator: true);

        var createResponse = await client.PutAsJsonAsync("/api/ClassConfiguration/rooms",
            new { room4SeatCount = 120, room1SeatCount = 30, room6SeatCount = 40 });
        createResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await createResponse.Content.ReadFromJsonAsync<RoomConfigurationDto>())!
            .ShouldBe(new RoomConfigurationDto(120, 30, 40));

        var updateResponse = await client.PutAsJsonAsync("/api/ClassConfiguration/rooms",
            new { room4SeatCount = 135, room1SeatCount = 32, room6SeatCount = 45 });
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await client.GetAsync("/api/ClassConfiguration/rooms");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await getResponse.Content.ReadFromJsonAsync<RoomConfigurationDto>())!
            .ShouldBe(new RoomConfigurationDto(135, 32, 45));
    }

    [Test]
    public async Task SaveEndpoint_IncompleteOrNegativePayloadReturnsValidationProblemWithoutWriting()
    {
        using var client = await LoginAsync(administrator: true);

        var incomplete = await client.PutAsJsonAsync("/api/ClassConfiguration/rooms",
            new { room4SeatCount = 120, room1SeatCount = 30 });
        var negative = await client.PutAsJsonAsync("/api/ClassConfiguration/rooms",
            new { room4SeatCount = -1, room1SeatCount = 30, room6SeatCount = 40 });

        incomplete.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        incomplete.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        negative.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        negative.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await TestApp.CountAsync<skestock.Domain.Entities.SchoolClasses.SharedClassConfiguration>()).ShouldBe(0);
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
