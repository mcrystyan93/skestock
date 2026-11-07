using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertCongregation;
using skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;
using skestock.Domain.Entities.Congregations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.FruitDessertPlans;

public class InformarePdfTests : TestBase
{
    private HttpClient _client = null!;

    [SetUp]
    public async Task Login()
    {
        var userId = await TestApp.RunAsAdministratorAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
        _client = FunctionalTestSetup.Factory.CreateClient();
        var login = await _client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new { email = "administrator@local", password = "Administrator1234!" });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [TearDown]
    public void Dispose() => _client.Dispose();

    private static string Url(Guid classId, Guid entryId) =>
        $"/api/FruitDessertPlans/{classId}/entries/{entryId}/informare";

    // Returns the class and its first plan day, optionally assigned to a congregation.
    private static async Task<(Guid ClassId, Guid EntryId)> SeedDayAsync(
        bool withCongregation, ClassStatus status = ClassStatus.Active)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"cls-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 11, 2),
            EndDate = new DateOnly(2026, 11, 13),
            Status = ClassStatus.Active
        };
        await TestApp.AddAsync(schoolClass);
        var plan = await TestApp.SendAsync(new GetFruitDessertPlanQuery { ClassId = schoolClass.Id });
        var entryId = plan.Value[0].Id;
        if (withCongregation)
        {
            var congregation = new Congregation
            {
                Name = $"FT{Guid.NewGuid():N}"[..12],
                Email = "c@example.com",
                DessertContactPerson = "Maria",
                DessertContactPhone = "0722 123 456"
            };
            await TestApp.AddAsync(congregation);
            await TestApp.SendAsync(new SetFruitDessertCongregationCommand
            {
                ClassId = schoolClass.Id, EntryId = entryId, CongregationId = congregation.Id
            });
        }

        // A closed class rejects the assignment above, so close it only after seeding.
        if (status == ClassStatus.Closed)
            await FruitDessertPlanQueryTests.ExecuteAsync(c => c.SchoolClasses
                .Where(s => s.Id == schoolClass.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ClassStatus.Closed)));
        return (schoolClass.Id, entryId);
    }

    private static Task<int> MessageCountAsync() =>
        FruitDessertPlanQueryTests.ExecuteAsync(c => c.FruitDessertMessages.CountAsync());

    [Test]
    public async Task Get_WithCongregation_ReturnsPdfWithFileName_AndWritesNothing()
    {
        var (classId, entryId) = await SeedDayAsync(withCongregation: true);

        var response = await _client.GetAsync(Url(classId, entryId));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        response.Content.Headers.ContentDisposition!.FileName.ShouldBe("Informare-fructe-si-desert-2026-11-02.pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("%PDF");
        (await MessageCountAsync()).ShouldBe(0);
        (await FruitDessertPlanQueryTests.ExecuteAsync(c => c.FruitDessertPlanEntries.AnyAsync(e => e.EmailSent)))
            .ShouldBeFalse();
    }

    [Test]
    public async Task Get_WithoutCongregation_ReturnsConflict()
    {
        var (classId, entryId) = await SeedDayAsync(withCongregation: false);

        var response = await _client.GetAsync(Url(classId, entryId));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await MessageCountAsync()).ShouldBe(0);
    }

    [Test]
    public async Task Get_ClosedClass_StillReturnsPdf()
    {
        var (classId, entryId) = await SeedDayAsync(withCongregation: true, ClassStatus.Closed);

        var response = await _client.GetAsync(Url(classId, entryId));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
