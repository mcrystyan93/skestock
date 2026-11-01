using System.Net;
using System.Net.Http.Json;
using skestock.Application.Features.Statistics.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;

namespace skestock.Application.FunctionalTests.Features.Statistics;

public class PurchaseSegmentsTests : TestBase
{
    private const string Url = "/api/Statistics/purchases/segments";

    private static async Task<Item> AddItemAsync(string name, Guid categoryId, bool active = true)
    {
        var item = new Item { Name = name, Unit = "kg", CategoryId = categoryId, IsActive = active };
        await TestApp.AddAsync(item);
        return item;
    }

    private static Task AddYearAsync(Item item, decimal? interval) =>
        TestApp.AddAsync(new ItemPurchaseStatistic
        {
            Scope = PurchaseStatisticsScope.Last365Days,
            ItemId = item.Id,
            AverageIntervalDays = interval,
            AverageQuantity = 4m,
            PurchaseCount = 6,
            ComputedAt = DateTimeOffset.UtcNow,
            LastPurchasedAt = DateTimeOffset.UtcNow
        });

    private static Task AddClassStatAsync(Item item, SchoolClass schoolClass) =>
        TestApp.AddAsync(new ItemPurchaseStatistic
        {
            Scope = PurchaseStatisticsScope.Class,
            ClassId = schoolClass.Id,
            ItemId = item.Id,
            AverageQuantity = 1m,
            PurchaseCount = 1,
            ComputedAt = DateTimeOffset.UtcNow,
            LastPurchasedAt = DateTimeOffset.UtcNow
        });

    [Test]
    public async Task Anonymous_call_is_rejected()
    {
        using var anonymous = FunctionalTestSetup.Factory.CreateClient();

        (await anonymous.GetAsync(Url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Returns_each_segment()
    {
        var prefix = $"FT{Guid.NewGuid():N}"[..10];
        var category = new Category { Name = $"{prefix}-Cat" };
        await TestApp.AddAsync(category);

        var weekly = await AddItemAsync($"{prefix}-Weekly", category.Id);
        var biweekly = await AddItemAsync($"{prefix}-Biweekly", category.Id);
        var monthly = await AddItemAsync($"{prefix}-Monthly", category.Id);
        var rare = await AddItemAsync($"{prefix}-Rare", category.Id);
        var perClass = await AddItemAsync($"{prefix}-PerClass", category.Id);
        var disabled = await AddItemAsync($"{prefix}-Disabled", category.Id, active: false);

        await AddYearAsync(weekly, 7m);
        await AddYearAsync(biweekly, 14m);
        await AddYearAsync(monthly, 30m);
        await AddYearAsync(rare, 90m);
        await AddYearAsync(disabled, 7m);

        for (var i = 0; i < 2; i++)
        {
            var schoolClass = new SchoolClass
            {
                Name = $"{prefix}-C{i}", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 2, 1)
            };
            await TestApp.AddAsync(schoolClass);
            await AddClassStatAsync(perClass, schoolClass);
        }

        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });
        using var client = FunctionalTestSetup.Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Users/login?useCookies=true",
            new { email = "test@local", password = "Testing1234!" });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);

        var response = await client.GetAsync(Url);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<PurchaseSegmentsDto>())!;
        dto.Weekly.Select(i => i.ItemId).ShouldBe([weekly.Id]);
        dto.EveryTwoWeeks.Select(i => i.ItemId).ShouldBe([biweekly.Id]);
        dto.Monthly.Select(i => i.ItemId).ShouldBe([monthly.Id]);
        dto.OncePerClass.Select(i => i.ItemId).ShouldBe([perClass.Id]);
        dto.Weekly[0].AverageQuantity.ShouldBe(4m);
        dto.Weekly[0].PurchaseCount.ShouldBe(6);
    }
}
