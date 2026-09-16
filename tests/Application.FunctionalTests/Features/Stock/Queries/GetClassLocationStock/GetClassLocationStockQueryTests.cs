using skestock.Application.Features.Stock.Queries.GetClassLocationStock;
using skestock.Application.Features.Stock.Commands.SetClassItemStockVisibility;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.Stock.Queries.GetClassLocationStock;

public class GetClassLocationStockQueryTests : TestBase
{
    /// <summary>
    /// Every test seeds its own Category/Location/SchoolClass tagged with a unique
    /// <see cref="_prefix"/> and item names built from it, so SearchTerm assertions stay correct
    /// regardless of leftover rows from other tests/fixtures, and cache keys stay distinct across
    /// tests against the same long-lived in-process HybridCache instance.
    /// </summary>
    private string _prefix = null!;
    private Category _category = null!;
    private Location _location = null!;
    private SchoolClass _schoolClass = null!;

    [SetUp]
    public async Task SetUpPrerequisites()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];

        _category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(_category);

        _location = new Location { Name = $"{_prefix}-Location", Type = "Kitchen" };
        await TestApp.AddAsync(_location);

        _schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Class",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 6, 1)
        };
        await TestApp.AddAsync(_schoolClass);
    }

    private async Task<Item> SeedItemAsync(
        string name,
        int minThreshold = 5,
        bool isPerishable = false)
    {
        var item = new Item
        {
            Name = $"{_prefix}-{name}",
            Unit = "unit",
            MinThreshold = minThreshold,
            IsPerishable = isPerishable,
            CategoryId = _category.Id
        };
        await TestApp.AddAsync(item);
        return item;
    }

    private async Task<GoodsReceipt> SeedGoodsReceiptAsync()
    {
        // Only the FK scalar is set (not the Class navigation): _schoolClass was persisted via a
        // previous TestApp.AddAsync call using a different DbContext scope, so it's a detached
        // instance here. Assigning it as a navigation would make EF's Add() cascade Added state
        // onto it too, causing SaveChangesAsync to try to re-insert an already-existing row.
        var receipt = new GoodsReceipt { ClassId = _schoolClass.Id, Class = null!, Note = $"{_prefix}-Receipt" };
        await TestApp.AddAsync(receipt);
        return receipt;
    }

    private async Task SeedBatchAsync(
        Item item,
        GoodsReceipt receipt,
        int quantity,
        DateOnly? expiryDate = null)
    {
        await SeedBatchAsync(item, receipt, _location.Id, quantity, expiryDate);
    }

    private async Task SeedBatchAsync(
        Item item,
        GoodsReceipt receipt,
        Guid locationId,
        int quantity,
        DateOnly? expiryDate = null)
    {
        // Only FK scalars are set here (Item/Location/ReceivedClass/GoodsReceipt navigations are
        // left at their null! default) - item/receipt/location/schoolClass were persisted via
        // previous TestApp.AddAsync calls using different DbContext scopes, so they're detached
        // here. Assigning them as navigations would make EF's Add() cascade Added state onto
        // them too, causing SaveChangesAsync to try to re-insert already-existing rows.
        var batch = new StockBatch
        {
            ItemId = item.Id,
            LocationId = locationId,
            ReceivedClassId = _schoolClass.Id,
            GoodsReceiptId = receipt.Id,
            Quantity = quantity,
            ExpiryDate = expiryDate,
            UnitPrice = 1m,
            ReceivedDate = new DateOnly(2024, 1, 1)
        };

        await TestApp.AddAsync(batch);
    }

    private static ColumnFilter EqualsFilter(string field, Guid value) =>
        new(field, FilterOperator.Equals, value);

    [Test]
    public async Task Handle_SearchTermMatchingItemName_ReturnsOnlyMatchingItems()
    {
        var flour = await SeedItemAsync("Flour");
        var pasta = await SeedItemAsync("Pasta");
        var receipt = await SeedGoodsReceiptAsync();
        await SeedBatchAsync(flour, receipt, 10);
        await SeedBatchAsync(pasta, receipt, 6);

        var result = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id,
            Filters = [EqualsFilter("locationId", _location.Id)],
            SearchTerm = $"{_prefix}-Flour"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemName.ShouldBe(flour.Name);
    }

    [Test]
    public async Task Handle_SearchTermIsCaseInsensitive_ReturnsMatchingItem()
    {
        var flour = await SeedItemAsync("Flour");
        var receipt = await SeedGoodsReceiptAsync();
        await SeedBatchAsync(flour, receipt, 10);

        var result = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id,
            Filters = [EqualsFilter("locationId", _location.Id)],
            SearchTerm = "flour"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemName.ShouldBe(flour.Name);
    }

    [Test]
    public async Task Handle_ExpiredPerishableBatch_ReturnsRowAndReportExpiryFlags()
    {
        var milk = await SeedItemAsync("Milk", isPerishable: true);
        var receipt = await SeedGoodsReceiptAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await SeedBatchAsync(milk, receipt, 10, today.AddDays(-1));

        var result = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id,
            Filters = [EqualsFilter("locationId", _location.Id)]
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.HasExpiredItems.ShouldBeTrue();
        result.Value.Items.Single().IsExpired.ShouldBeTrue();
        result.Value.Items.Single().ExpiredQuantity.ShouldBe(10);
    }

    [Test]
    public async Task Handle_SearchTermWithNoMatches_ReturnsEmptyList()
    {
        var flour = await SeedItemAsync("Flour");
        var receipt = await SeedGoodsReceiptAsync();
        await SeedBatchAsync(flour, receipt, 10);

        var result = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id,
            Filters = [EqualsFilter("locationId", _location.Id)],
            SearchTerm = $"{_prefix}-does-not-exist"
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_NoFilters_ReturnsEveryItemForTheClass()
    {
        var flour = await SeedItemAsync("Flour");
        var pasta = await SeedItemAsync("Pasta");
        var receipt = await SeedGoodsReceiptAsync();
        await SeedBatchAsync(flour, receipt, 10);
        await SeedBatchAsync(pasta, receipt, 6);

        var result = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items.Select(x => x.ItemName).ShouldBe([flour.Name, pasta.Name], ignoreOrder: true);
    }

    [Test]
    public async Task Handle_LocationAndCategoryFilters_ReturnOnlyMatchingStock()
    {
        var flour = await SeedItemAsync("Flour");
        var otherCategory = new Category { Name = $"{_prefix}-OtherCategory" };
        await TestApp.AddAsync(otherCategory);
        var pasta = new Item
        {
            Name = $"{_prefix}-Pasta",
            Unit = "unit",
            MinThreshold = 5,
            CategoryId = otherCategory.Id
        };
        await TestApp.AddAsync(pasta);

        var otherLocation = new Location { Name = $"{_prefix}-OtherLocation", Type = "Storage" };
        await TestApp.AddAsync(otherLocation);

        var receipt = await SeedGoodsReceiptAsync();
        await SeedBatchAsync(flour, receipt, _location.Id, 10);
        await SeedBatchAsync(flour, receipt, otherLocation.Id, 7);
        await SeedBatchAsync(pasta, receipt, _location.Id, 6);

        var result = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id,
            Filters =
            [
                EqualsFilter("locationId", _location.Id),
                EqualsFilter("categoryId", _category.Id)
            ]
        });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items.Single().ItemId.ShouldBe(flour.Id);
        result.Value.Items.Single().LocationId.ShouldBe(_location.Id);
        result.Value.Items.Single().Quantity.ShouldBe(10);
    }

    [Test]
    public async Task Handle_MarkedZeroStock_IsHiddenFromNormalReportAndReturnedWhenIncluded()
    {
        var item = await SeedItemAsync("Depleted");
        var receipt = await SeedGoodsReceiptAsync();
        await SeedBatchAsync(item, receipt, 0);

        var userId = await TestApp.RunAsDefaultUserAsync();
        await TestApp.AddAsync(new UserProfile
        {
            IdentityId = userId!.Value,
            FirstName = "Test",
            LastName = "User"
        });

        var visibilityResult = await TestApp.SendAsync(new SetClassItemStockVisibilityCommand
        {
            ClassId = _schoolClass.Id,
            ItemId = item.Id,
            HideWhenZeroStock = true
        });
        visibilityResult.IsSuccess.ShouldBeTrue();

        var normalResult = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id,
            Filters = [EqualsFilter("locationId", _location.Id)]
        });
        var hiddenResult = await TestApp.SendAsync(new GetClassLocationStockQuery
        {
            ClassId = _schoolClass.Id,
            Filters = [EqualsFilter("locationId", _location.Id)],
            IncludeHidden = true
        });

        normalResult.IsSuccess.ShouldBeTrue();
        normalResult.Value.Items.ShouldBeEmpty();
        hiddenResult.IsSuccess.ShouldBeTrue();
        hiddenResult.Value.Items.Single().ItemId.ShouldBe(item.Id);
        hiddenResult.Value.Items.Single().HideWhenZeroStock.ShouldBeTrue();
    }
}
