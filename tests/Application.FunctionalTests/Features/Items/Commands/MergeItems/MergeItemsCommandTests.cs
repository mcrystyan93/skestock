using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using skestock.Application.Features.Items.Commands.MergeItems.Mergers;
using skestock.Application.Features.Items.Queries.GetItemMergePreview;
using skestock.Domain.Entities.Categories;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.OrderLists;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Statistics;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Entities.SupplyLists;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;
using skestock.Infrastructure.Data;

namespace skestock.Application.FunctionalTests.Features.Items.Commands.MergeItems;

/// <summary>
/// Real SQL Server run of the merge over data that collides in every unique-keyed table, including closed-class data.
/// Proves EF orders the deletes before the conflicting re-points inside one SaveChanges.
/// </summary>
public class MergeItemsCommandTests : TestBase
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly DateOnly Day = new(2026, 9, 10);

    private Item _primary = null!, _s1 = null!, _s2 = null!;
    private SchoolClass _open = null!, _closed = null!;
    private Location _l1 = null!, _l2 = null!;
    private SupplyList _listX = null!, _listY = null!;

    private async Task SeedAsync()
    {
        var category = new Category { Name = $"FT{Guid.NewGuid():N}"[..10] };
        await TestApp.AddAsync(category);
        _primary = await AddItemAsync("Primary", category);
        _s1 = await AddItemAsync("Secondary1", category);
        _s2 = await AddItemAsync("Secondary2", category);
        _open = await AddClassAsync("open", ClassStatus.Active);
        _closed = await AddClassAsync("closed", ClassStatus.Closed);
        _l1 = new Location { Name = "L1", Type = "StorageRoom" };
        _l2 = new Location { Name = "L2", Type = "StorageRoom" };
        await TestApp.AddAsync(_l1);
        await TestApp.AddAsync(_l2);
        _listX = new SupplyList { Name = "X" };
        _listY = new SupplyList { Name = "Y" };
        await TestApp.AddAsync(_listX);
        await TestApp.AddAsync(_listY);
    }

    private static async Task<Item> AddItemAsync(string name, Category category)
    {
        var item = new Item { Name = $"{name}-{Guid.NewGuid():N}"[..20], Unit = "kg", MinThreshold = 1, CategoryId = category.Id };
        await TestApp.AddAsync(item);
        return item;
    }

    private static async Task<SchoolClass> AddClassAsync(string name, ClassStatus status)
    {
        var schoolClass = new SchoolClass
        {
            Name = $"{name}-{Guid.NewGuid():N}"[..20],
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20),
            Status = status
        };
        await TestApp.AddAsync(schoolClass);
        return schoolClass;
    }

    /// <summary>Collisions: primary-vs-secondary and secondary-vs-secondary in every unique-keyed table.</summary>
    private async Task SeedReferencesAsync(Guid userId)
    {
        async Task Add<T>(T entity) where T : class => await TestApp.AddAsync(entity);

        // Stock: open class (P + S2) and closed class (S1).
        await Add(new StockBatch { ItemId = _primary.Id, LocationId = _l1.Id, ReceivedClassId = _open.Id, Quantity = 10, UnitPrice = 2m, ReceivedDate = Day });
        await Add(new StockBatch { ItemId = _s2.Id, LocationId = _l1.Id, ReceivedClassId = _open.Id, Quantity = 6, UnitPrice = 2m, ReceivedDate = Day });
        await Add(new StockBatch { ItemId = _s1.Id, LocationId = _l2.Id, ReceivedClassId = _closed.Id, Quantity = 4, UnitPrice = 2m, ReceivedDate = Day });
        await Add(new StockTransaction
        {
            ItemId = _s1.Id, LocationId = _l2.Id, ClassId = _closed.Id, UserId = userId,
            Type = StockTransactionType.Usage, QuantityChange = -1, CreatedAt = DateTimeOffset.UtcNow
        });

        // ClassItemStockVisibility (ClassId, LocationId)
        await Add(new ClassItemStockVisibility { ItemId = _primary.Id, ClassId = _open.Id, LocationId = _l1.Id });
        await Add(new ClassItemStockVisibility { ItemId = _s1.Id, ClassId = _open.Id, LocationId = _l1.Id });
        await Add(new ClassItemStockVisibility { ItemId = _s1.Id, ClassId = _open.Id, LocationId = _l2.Id });
        await Add(new ClassItemStockVisibility { ItemId = _s2.Id, ClassId = _open.Id, LocationId = _l2.Id });
        await Add(new ClassItemStockVisibility { ItemId = _s1.Id, ClassId = _closed.Id, LocationId = _l1.Id });

        // ItemLocationThreshold (LocationId)
        await Add(new ItemLocationThreshold { ItemId = _primary.Id, LocationId = _l1.Id, MinThreshold = 1 });
        await Add(new ItemLocationThreshold { ItemId = _s1.Id, LocationId = _l1.Id, MinThreshold = 2 });
        await Add(new ItemLocationThreshold { ItemId = _s1.Id, LocationId = _l2.Id, MinThreshold = 3 });
        await Add(new ItemLocationThreshold { ItemId = _s2.Id, LocationId = _l2.Id, MinThreshold = 4 });

        // SupplyListLine (SupplyListId), quantities sum per list
        await Add(new SupplyListLine { ItemId = _primary.Id, SupplyListId = _listX.Id, Unit = "kg", Quantity = 1 });
        await Add(new SupplyListLine { ItemId = _s1.Id, SupplyListId = _listX.Id, Unit = "kg", Quantity = 2 });
        await Add(new SupplyListLine { ItemId = _s1.Id, SupplyListId = _listY.Id, Unit = "kg", Quantity = 3 });
        await Add(new SupplyListLine { ItemId = _s2.Id, SupplyListId = _listY.Id, Unit = "kg", Quantity = 4 });

        // DailyItemConsumption (Date, ClassId, LocationId)
        await Add(new DailyItemConsumption { ItemId = _primary.Id, Date = Day, ClassId = _open.Id, LocationId = _l1.Id, Quantity = 1, TotalValue = 2m, ComputedAt = DateTimeOffset.UtcNow });
        await Add(new DailyItemConsumption { ItemId = _s1.Id, Date = Day, ClassId = _open.Id, LocationId = _l1.Id, Quantity = 2, TotalValue = 4m, ComputedAt = DateTimeOffset.UtcNow });
        await Add(new DailyItemConsumption { ItemId = _s1.Id, Date = Day, ClassId = _closed.Id, LocationId = _l2.Id, Quantity = 3, TotalValue = 6m, ComputedAt = DateTimeOffset.UtcNow });
        await Add(new DailyItemConsumption { ItemId = _s2.Id, Date = Day, ClassId = _closed.Id, LocationId = _l2.Id, Quantity = 4, TotalValue = 8m, ComputedAt = DateTimeOffset.UtcNow });

        // ItemPurchaseStatistic (Scope, ClassId) incl. null ClassId
        await Add(Stat(_primary, PurchaseStatisticsScope.Last365Days, null, 10));
        await Add(Stat(_s1, PurchaseStatisticsScope.Last365Days, null, 5));
        await Add(Stat(_s1, PurchaseStatisticsScope.Class, _closed.Id, 3));
        await Add(Stat(_s2, PurchaseStatisticsScope.Class, _closed.Id, 4));

        // ClassBalance (ClassId, LocationId), per-class totals must not change
        await Add(new ClassBalance { ItemId = _primary.Id, ClassId = _closed.Id, LocationId = _l2.Id, OpeningQty = 5, ClosingQty = 5 });
        await Add(new ClassBalance { ItemId = _s1.Id, ClassId = _closed.Id, LocationId = _l2.Id, OpeningQty = 3, ClosingQty = null });

        var order = new OrderList { ClassId = _open.Id };
        await Add(order);
        await Add(new OrderListLine { OrderListId = order.Id, ItemId = _s1.Id, ProductName = "snapshot name" });
    }

    private static ItemPurchaseStatistic Stat(Item item, PurchaseStatisticsScope scope, Guid? classId, int quantity) => new()
    {
        ItemId = item.Id, Scope = scope, ClassId = classId, TotalQuantity = quantity, TotalValue = quantity * 2m,
        PurchaseCount = 1, AverageQuantity = quantity, AverageUnitPrice = 2m,
        LastPurchasedAt = DateTimeOffset.UtcNow, ComputedAt = DateTimeOffset.UtcNow
    };

    private static async Task<(HttpClient Client, Guid UserId)> LoginAsync(bool administrator)
    {
        var userId = administrator ? await TestApp.RunAsAdministratorAsync() : await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile { IdentityId = userId!.Value, FirstName = "Staff", LastName = "Member" });

        var client = FunctionalTestSetup.Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Users/login?useCookies=true", new
        {
            email = administrator ? "administrator@local" : "test@local",
            password = administrator ? "Administrator1234!" : "Testing1234!"
        });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (client, userId.Value);
    }

    private static async Task<List<T>> QueryAsync<T>() where T : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Set<T>().AsNoTracking().ToListAsync();
    }

    private object Body(params Item[] items) => new { primaryItemId = _primary.Id, itemIds = items.Select(i => i.Id) };

    private static Dictionary<(Guid, Guid), int> StockByClassAndLocation(IEnumerable<StockBatch> batches) =>
        batches.GroupBy(b => (b.ReceivedClassId, b.LocationId)).ToDictionary(g => g.Key, g => g.Sum(b => b.Quantity));

    [Test]
    public async Task Merge_AsAdministrator_CombinesCollisionsAndRepointsClosedClassData()
    {
        await SeedAsync();
        var (client, userId) = await LoginAsync(administrator: true);
        using var _ = client;
        await SeedReferencesAsync(userId);
        var stockBefore = StockByClassAndLocation(await QueryAsync<StockBatch>());
        var balanceBefore = (await QueryAsync<ClassBalance>()).Sum(b => b.OpeningQty);

        var response = await client.PostAsJsonAsync("/api/Items/merge", Body(_primary, _s1, _s2));

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
        var secondaries = new[] { _s1.Id, _s2.Id };
        (await QueryAsync<Item>()).Where(i => secondaries.Contains(i.Id)).ShouldAllBe(i => !i.IsActive);
        (await QueryAsync<Item>()).Single(i => i.Id == _primary.Id).IsActive.ShouldBeTrue();

        var batches = await QueryAsync<StockBatch>();
        batches.ShouldAllBe(b => b.ItemId == _primary.Id);
        StockByClassAndLocation(batches).ShouldBe(stockBefore);
        (await QueryAsync<StockTransaction>()).ShouldAllBe(t => t.ItemId == _primary.Id);
        (await QueryAsync<OrderListLine>()).Single().ItemId.ShouldBe(_primary.Id);

        var visibility = await QueryAsync<ClassItemStockVisibility>();
        visibility.ShouldAllBe(v => v.ItemId == _primary.Id);
        visibility.Count.ShouldBe(3); // (open,L1) (open,L2) (closed,L1)

        var thresholds = await QueryAsync<ItemLocationThreshold>();
        thresholds.ShouldAllBe(t => t.ItemId == _primary.Id);
        thresholds.Count.ShouldBe(2);
        thresholds.Single(t => t.LocationId == _l1.Id).MinThreshold.ShouldBe(1); // primary's row wins

        var lines = await QueryAsync<SupplyListLine>();
        lines.ShouldAllBe(l => l.ItemId == _primary.Id);
        lines.Single(l => l.SupplyListId == _listX.Id).Quantity.ShouldBe(3m);
        lines.Single(l => l.SupplyListId == _listY.Id).Quantity.ShouldBe(7m);

        var consumption = await QueryAsync<DailyItemConsumption>();
        consumption.ShouldAllBe(c => c.ItemId == _primary.Id);
        consumption.Single(c => c.ClassId == _open.Id).Quantity.ShouldBe(3);
        consumption.Single(c => c.ClassId == _closed.Id).TotalValue.ShouldBe(14m);

        var stats = await QueryAsync<ItemPurchaseStatistic>();
        stats.ShouldAllBe(s => s.ItemId == _primary.Id);
        stats.Single(s => s.Scope == PurchaseStatisticsScope.Last365Days).TotalQuantity.ShouldBe(15);
        stats.Single(s => s.Scope == PurchaseStatisticsScope.Class).TotalQuantity.ShouldBe(7);

        var balances = await QueryAsync<ClassBalance>();
        balances.ShouldAllBe(b => b.ItemId == _primary.Id);
        balances.Count.ShouldBe(1);
        balances.Sum(b => b.OpeningQty).ShouldBe(balanceBefore);
    }

    [Test]
    public async Task Merge_IsIdempotent()
    {
        await SeedAsync();
        var (client, userId) = await LoginAsync(administrator: true);
        using var _ = client;
        await SeedReferencesAsync(userId);

        (await client.PostAsJsonAsync("/api/Items/merge", Body(_primary, _s1, _s2))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync("/api/Items/merge", Body(_primary, _s1, _s2))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task MergeAndPreview_RejectNonAdministrators()
    {
        await SeedAsync();
        var (client, userId) = await LoginAsync(administrator: false);
        using var _ = client;
        await SeedReferencesAsync(userId);

        (await client.PostAsJsonAsync("/api/Items/merge", Body(_primary, _s1))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await client.PostAsJsonAsync("/api/Items/merge-preview", Body(_primary, _s1))).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        (await QueryAsync<Item>()).ShouldAllBe(i => i.IsActive);
        (await QueryAsync<StockBatch>()).Select(b => b.ItemId).Distinct().Count().ShouldBe(3);
    }

    [Test]
    public async Task Merge_WithInvalidSetOrInactivePrimary_FailsAndChangesNothing()
    {
        await SeedAsync();
        var (client, userId) = await LoginAsync(administrator: true);
        using var _ = client;
        await SeedReferencesAsync(userId);

        (await client.PostAsJsonAsync("/api/Items/merge", Body(_primary))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/Items/merge-preview", Body(_primary))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await client.PostAsJsonAsync("/api/Items/merge", new { primaryItemId = _primary.Id, itemIds = new[] { _primary.Id, Guid.NewGuid() } }))
            .IsSuccessStatusCode.ShouldBeFalse();

        (await client.PatchAsync($"/api/Items/{_primary.Id}/disable", null)).IsSuccessStatusCode.ShouldBeTrue();
        (await client.PostAsJsonAsync("/api/Items/merge", Body(_primary, _s1))).IsSuccessStatusCode.ShouldBeFalse();

        (await QueryAsync<StockBatch>()).Select(b => b.ItemId).Distinct().Count().ShouldBe(3);
        (await QueryAsync<Item>()).Single(i => i.Id == _s1.Id).IsActive.ShouldBeTrue();
        (await QueryAsync<SupplyListLine>()).Count.ShouldBe(4);
    }

    [Test]
    public async Task Preview_ReportsTheCountsTheMergeApplies()
    {
        await SeedAsync();
        var (client, userId) = await LoginAsync(administrator: true);
        using var _ = client;
        await SeedReferencesAsync(userId);
        var rowsBefore = (await QueryAsync<SupplyListLine>()).Count;

        var response = await client.PostAsJsonAsync("/api/Items/merge-preview", Body(_primary, _s1, _s2));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var preview = (await response.Content.ReadFromJsonAsync<ItemMergePreviewDto>(Json))!;
        (await QueryAsync<SupplyListLine>()).Count.ShouldBe(rowsBefore); // nothing written
        var supply = preview.Impacts.Single(i => i.Kind == ItemMergeKind.SupplyListLines);
        preview.Impacts.Single(i => i.Kind == ItemMergeKind.StockBatches).Relinked.ShouldBe(2);

        await client.PostAsJsonAsync("/api/Items/merge", Body(_primary, _s1, _s2));
        (await QueryAsync<SupplyListLine>()).Count.ShouldBe(rowsBefore - supply.Combined);
    }
}
