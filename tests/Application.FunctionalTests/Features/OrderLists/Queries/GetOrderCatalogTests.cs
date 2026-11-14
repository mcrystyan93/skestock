using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using skestock.Application.Features.OrderLists.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.OrderLists.Queries;

public class GetOrderCatalogTests : TestBase
{
    [Test]
    public async Task Anonymous_call_is_rejected()
    {
        using var anonymous = FunctionalTestSetup.Factory.CreateClient();

        (await anonymous.GetAsync($"/api/OrderLists/catalog/{Guid.NewGuid()}")).StatusCode
            .ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Unknown_class_returns_not_found()
    {
        using var client = await LoginAsync();

        (await client.GetAsync($"/api/OrderLists/catalog/{Guid.NewGuid()}")).StatusCode
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Returns_active_items_with_locations_low_stock_and_statistics()
    {
        var prefix = $"FT{Guid.NewGuid():N}"[..10];
        var category = new Category { Name = $"{prefix}-Cat" };
        await TestApp.AddAsync(category);
        var kitchen = new Location { Name = $"{prefix}-Kitchen", Type = "Kitchen" };
        var pantry = new Location { Name = $"{prefix}-Pantry", Type = "Storage" };
        var home = new Location { Name = $"{prefix}-Home", Type = "Storage" };
        await TestApp.AddAsync(kitchen);
        await TestApp.AddAsync(pantry);
        await TestApp.AddAsync(home);
        var schoolClass = new SchoolClass
        {
            Name = $"{prefix}-Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);

        var rice = new Item { Name = $"{prefix}-Rice", Unit = "kg", CategoryId = category.Id };
        var salt = new Item { Name = $"{prefix}-Salt", Unit = "kg", CategoryId = category.Id, LocationId = home.Id };
        var off = new Item { Name = $"{prefix}-Off", Unit = "kg", CategoryId = category.Id, IsActive = false };
        await TestApp.AddAsync(rice);
        await TestApp.AddAsync(salt);
        await TestApp.AddAsync(off);

        await TestApp.AddAsync(new ItemLocationThreshold { ItemId = rice.Id, LocationId = kitchen.Id, MinThreshold = 5 });
        await TestApp.AddAsync(new ItemLocationThreshold { ItemId = rice.Id, LocationId = pantry.Id, MinThreshold = 5 });

        var receipt = new GoodsReceipt { ClassId = schoolClass.Id, Class = null!, Note = $"{prefix}-Receipt" };
        await TestApp.AddAsync(receipt);
        foreach (var (location, quantity) in new[] { (kitchen, 2), (pantry, 20) })
        {
            await TestApp.AddAsync(new StockBatch
            {
                ItemId = rice.Id, LocationId = location.Id, ReceivedClassId = schoolClass.Id,
                GoodsReceiptId = receipt.Id, Quantity = quantity, UnitPrice = 1m, ReceivedDate = new DateOnly(2026, 2, 1)
            });
        }

        await TestApp.AddAsync(new ItemPurchaseStatistic
        {
            Scope = PurchaseStatisticsScope.Last365Days, ItemId = rice.Id, AverageIntervalDays = 7m,
            LastQuantity = 12, AverageQuantity = 10m, PurchaseCount = 6,
            ComputedAt = DateTimeOffset.UtcNow, LastPurchasedAt = DateTimeOffset.UtcNow
        });

        using var client = await LoginAsync();

        var response = await client.GetAsync($"/api/OrderLists/catalog/{schoolClass.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var catalog = (await response.Content.ReadFromJsonAsync<List<OrderCatalogItemDto>>())!
            .Where(i => i.Name.StartsWith(prefix)).ToList();
        catalog.Select(i => i.Name).ShouldBe([$"{prefix}-Rice", $"{prefix}-Salt"]);

        var riceDto = catalog[0];
        riceDto.LastQuantity.ShouldBe(12);
        riceDto.PurchaseCount.ShouldBe(6);
        riceDto.AverageIntervalDays.ShouldBe(7m);
        riceDto.IsFrequent.ShouldBeTrue();
        riceDto.Locations.Select(l => (l.LocationName, l.IsLowStock))
            .ShouldBe([($"{prefix}-Kitchen", true), ($"{prefix}-Pantry", false)]);

        var saltDto = catalog[1];
        saltDto.LastQuantity.ShouldBeNull();
        saltDto.IsFrequent.ShouldBeFalse();
        saltDto.Locations.ShouldHaveSingleItem().LocationName.ShouldBe($"{prefix}-Home");
    }

    [Test]
    public async Task Json_uses_the_camel_case_client_contract()
    {
        var prefix = $"FT{Guid.NewGuid():N}"[..10];
        var category = new Category { Name = $"{prefix}-Cat" };
        await TestApp.AddAsync(category);
        var schoolClass = new SchoolClass
        {
            Name = $"{prefix}-Class", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 6, 1)
        };
        await TestApp.AddAsync(schoolClass);
        await TestApp.AddAsync(new Item { Name = $"{prefix}-Rice", Unit = "kg", CategoryId = category.Id });
        using var client = await LoginAsync();

        using var json = JsonDocument.Parse(await client.GetStringAsync($"/api/OrderLists/catalog/{schoolClass.Id}"));

        var item = json.RootElement.EnumerateArray().Single(e => e.GetProperty("name").GetString()!.StartsWith(prefix));
        item.EnumerateObject().Select(p => p.Name).Order().ShouldBe(
            ["averageIntervalDays", "imageFileId", "isFrequent", "itemId", "lastPurchasedAt", "lastQuantity", "locations", "name", "purchaseCount", "sku", "unit"]);
    }

    private static async Task<HttpClient> LoginAsync()
    {
        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
        var client = FunctionalTestSetup.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new { email = "test@local", password = "Testing1234!" });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        return client;
    }
}
