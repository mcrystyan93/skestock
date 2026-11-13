using System.Net;
using System.Net.Http.Json;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Statistics;

public class TopDonationsTests : TestBase
{
    private static string Url(Guid classId) => $"/api/Statistics/donations/top?scope=Class&classId={classId}";

    // The default user is already signed in via RunAsDefaultUserAsync; this only does the cookie login.
    private static async Task<HttpClient> LoginAsync()
    {
        var client = FunctionalTestSetup.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new { email = "test@local", password = "Testing1234!" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }

    [Test]
    public async Task Anonymous_call_is_rejected()
    {
        using var anonymous = FunctionalTestSetup.Factory.CreateClient();

        (await anonymous.GetAsync(Url(Guid.NewGuid()))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Counts_donations_but_not_orders()
    {
        var prefix = $"FT{Guid.NewGuid():N}"[..10];
        var category = new Category { Name = $"{prefix}-Cat" };
        await TestApp.AddAsync(category);
        var donated = new Item { Name = $"{prefix}-Donated", Unit = "kg", CategoryId = category.Id };
        var bought = new Item { Name = $"{prefix}-Bought", Unit = "kg", CategoryId = category.Id };
        await TestApp.AddAsync(donated);
        await TestApp.AddAsync(bought);
        var location = new Location { Name = $"{prefix}-Storage", Type = "StorageRoom" };
        await TestApp.AddAsync(location);
        var schoolClass = new SchoolClass
        {
            Name = $"{prefix}-Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31)
        };
        await TestApp.AddAsync(schoolClass);
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        foreach (var (item, type, quantity) in new[]
                 {
                     (donated, StockTransactionType.Donation, 5),
                     (donated, StockTransactionType.Donation, 3),
                     (bought, StockTransactionType.Order, 100)
                 })
        {
            await TestApp.AddAsync(new StockTransaction
            {
                ItemId = item.Id,
                LocationId = location.Id,
                ClassId = schoolClass.Id,
                UserId = userId.Value,
                Type = type,
                QuantityChange = quantity
            });
        }

        using var client = await LoginAsync();

        var response = await client.GetAsync(Url(schoolClass.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<TopPurchasesDto>())!;
        dto.ByQuantity.Select(s => (s.ItemId, s.TotalQuantity, s.PurchaseCount)).ShouldBe([(donated.Id, 8, 2)]);
    }
}
