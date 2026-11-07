using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertCongregation;
using skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;
using skestock.Application.Features.FruitDessertTemplates.Models;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.FruitDessertTemplates;

public class MessageTemplateTests : TestBase
{
    private const string Url = "/api/FruitDessertTemplates";
    private MessageTemplatesDto _original = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task Login()
    {
        var userId = await TestApp.RunAsAdministratorAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
        _client = await LoginAsync("administrator@local", "Administrator1234!");
        _original = (await GetAsync())!;
    }

    private static async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = FunctionalTestSetup.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Users/login?useCookies=true", new { email, password });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    // MessageTemplates is excluded from Respawn, so put the seeded text back.
    [TearDown]
    public async Task Restore()
    {
        foreach (var t in _original.Templates)
            await _client.PutAsJsonAsync($"{Url}/{t.Type}", new { t.Name, t.Body });
        _client.Dispose();

        // PUT stamps LastModifiedById with the test user; Respawn deletes that profile, so detach it
        // (the FK would otherwise block the next reset).
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        await db.MessageTemplates.ExecuteUpdateAsync(s => s.SetProperty(t => t.LastModifiedById, (Guid?)null));
    }

    // The API writes enums as camelCase strings (see Web/DependencyInjection.cs).
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private Task<MessageTemplatesDto?> GetAsync() => _client.GetFromJsonAsync<MessageTemplatesDto>(Url, JsonOptions);

    [Test]
    public async Task Get_ReturnsThreeTemplatesAndFourPlaceholders()
    {
        var dto = (await GetAsync())!;

        dto.Templates.Select(t => t.Type).ShouldBe([FruitDessertMessageType.Reminder, FruitDessertMessageType.Thanks, FruitDessertMessageType.Informare],
            ignoreOrder: true);
        dto.Placeholders.Count.ShouldBe(4);
    }

    [Test]
    public async Task NonAdmin_CanGetButPutIsForbiddenAndLeavesTemplateUnchanged()
    {
        await TestApp.RunAsDefaultUserAsync();
        using var nonAdmin = await LoginAsync("test@local", "Testing1234!");

        var get = await nonAdmin.GetAsync(Url);
        var put = await nonAdmin.PutAsJsonAsync($"{Url}/Reminder", new { Name = "Hack", Body = "<p>Hack</p>" });

        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        put.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var after = (await GetAsync())!.Templates.Single(t => t.Type == FruitDessertMessageType.Reminder);
        after.ShouldBe(_original.Templates.Single(t => t.Type == FruitDessertMessageType.Reminder));
    }

    [Test]
    public async Task Put_PersistsSanitizedHtml()
    {
        var response = await _client.PutAsJsonAsync($"{Url}/Reminder",
            new { Name = "Nou", Body = "<p onclick=\"x()\">Salut <strong>{ContactPerson}</strong></p><script>alert(1)</script>" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var saved = (await GetAsync())!.Templates.Single(t => t.Type == FruitDessertMessageType.Reminder);
        saved.Name.ShouldBe("Nou");
        saved.Body.ShouldContain("<strong>{ContactPerson}</strong>");
        saved.Body.ShouldNotContain("onclick");
        saved.Body.ShouldNotContain("<script");
    }

    [TestCase("reminder", FruitDessertMessageType.Reminder)]
    [TestCase("thanks", FruitDessertMessageType.Thanks)]
    public async Task Put_LowercaseRoute_PersistsBody(string route, FruitDessertMessageType type)
    {
        var response = await _client.PutAsJsonAsync($"{Url}/{route}", new { Name = "Nou", Body = "<p>Salut</p>" });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var saved = (await GetAsync())!.Templates.Single(t => t.Type == type);
        saved.Name.ShouldBe("Nou");
        saved.Body.ShouldContain("Salut");
    }

    [Test]
    public async Task Put_UnknownType_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync($"{Url}/bogus", new { Name = "Nou", Body = "<p>Salut</p>" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Put_UnknownPlaceholder_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync($"{Url}/Reminder", new { Name = "Nou", Body = "<p>{Foo}</p>" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Put_BodyOver4000_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync($"{Url}/Reminder", new { Name = "Nou", Body = new string('a', 4001) });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task SendAfterEdit_StoresNewTextInWhatsAppMarkupWithSubstitutedValues()
    {
        var schoolClass = new SchoolClass
        {
            Name = $"cls-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 11, 2),
            EndDate = new DateOnly(2026, 11, 13),
            Status = ClassStatus.Active
        };
        await TestApp.AddAsync(schoolClass);
        var congregation = new Congregation
        {
            Name = $"FT{Guid.NewGuid():N}"[..12],
            Email = "c@example.com",
            DessertContactPerson = "Maria",
            DessertContactPhone = "0722 123 456"
        };
        await TestApp.AddAsync(congregation);
        var plan = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });
        var entryId = plan.Value[0].Id;
        await TestApp.SendAsync(new SetFruitDessertCongregationCommand
        {
            ClassId = schoolClass.Id, EntryId = entryId, CongregationId = congregation.Id
        });
        (await _client.PutAsJsonAsync($"{Url}/Reminder",
            new { Name = "Nou", Body = "<p>Salut <strong>{ContactPerson}</strong> din {Congregation}</p>" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var sent = await TestApp.SendAsync(new SendFruitDessertMessageCommand
        {
            ClassId = schoolClass.Id, EntryId = entryId, Type = FruitDessertMessageType.Reminder
        });

        sent.IsSuccess.ShouldBeTrue();
        var messages = await Features.FruitDessertPlans.FruitDessertPlanQueryTests.ExecuteAsync(context =>
            context.FruitDessertMessages.Where(m => m.EntryId == entryId).ToListAsync());
        messages.Single().Body.ShouldBe($"Salut *Maria* din {congregation.Name}");
    }
}
