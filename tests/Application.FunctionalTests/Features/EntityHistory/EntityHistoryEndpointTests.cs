using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using skestock.Application.Features.Items.Commands.CreateItem;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Users;

namespace skestock.Application.FunctionalTests.Features.EntityHistory;

public class EntityHistoryEndpointTests : TestBase
{
    private static async Task<HttpClient> LoginAsync(bool administrator)
    {
        var userId = administrator ? await TestApp.RunAsAdministratorAsync() : await TestApp.RunAsDefaultUserAsync();
        // LastModifiedById is an FK to UserProfile.IdentityId.
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        var client = FunctionalTestSetup.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true", new
        {
            email = administrator ? "administrator@local" : "test@local",
            password = administrator ? "Administrator1234!" : "Testing1234!"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    [Test]
    public async Task Administrator_reads_item_versions_with_the_modifier_name()
    {
        var client = await LoginAsync(administrator: true);
        var category = new Category { Name = $"FT{Guid.NewGuid():N}"[..10] };
        await TestApp.AddAsync(category);
        var created = await TestApp.SendAsync(new CreateItemCommand { Name = "History-Old", Unit = "unit", CategoryId = category.Id });
        await TestApp.SendAsync(new EditItemCommand
        {
            Id = created.Value.Id, Name = "History-New", Unit = "unit", CategoryId = category.Id
        });

        var response = await client.GetAsync($"/api/EntityHistory/Items/{created.Value.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = body.RootElement.GetProperty("data");
        data.GetArrayLength().ShouldBe(2);
        data[0].GetProperty("values").GetProperty("Name").GetString().ShouldBe("History-New");
        data[0].GetProperty("modifiedByName").GetString().ShouldBe("Staff Member");
        data[0].GetProperty("operation").GetString().ShouldBe("updated");
        data[1].GetProperty("operation").GetString().ShouldBe("created");
    }

    [Test]
    public async Task Non_administrators_are_forbidden()
    {
        var client = await LoginAsync(administrator: false);

        var response = await client.GetAsync($"/api/EntityHistory/Items/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Unknown_entities_are_not_found()
    {
        var client = await LoginAsync(administrator: true);

        foreach (var entity in new[] { "AspNetUserTokens", "AspNetUserRoles" })
        {
            var response = await client.GetAsync($"/api/EntityHistory/{entity}/{Guid.NewGuid()}");

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }
    }
}
