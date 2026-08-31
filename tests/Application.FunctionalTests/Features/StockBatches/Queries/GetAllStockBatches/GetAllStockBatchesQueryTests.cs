using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Application.Features.StockBatches.Models;
using skestock.Application.Features.StockBatches.Queries.GetAllStockBatches;
using skestock.Domain.Entities;

namespace skestock.Application.FunctionalTests.Features.StockBatches.Queries.GetAllStockBatches;

public class GetAllStockBatchesQueryTests : TestBase
{
    /// <summary>
    /// Every test seeds its own Category/Item/Location/SchoolClass/GoodsReceipt tagged with a
    /// unique <see cref="_prefix"/> and always filters on it via <c>SearchTerm</c> (which matches
    /// Item/Location name). This keeps assertions correct regardless of leftover rows from other
    /// tests/fixtures, and keeps each test's cache key distinct so cached results from previous
    /// tests against the same long-lived in-process HybridCache instance can't bleed into this one.
    /// </summary>
    private string _prefix = null!;
    private Category _category = null!;
    private SchoolClass _schoolClass = null!;

    [SetUp]
    public async Task SetUpPrerequisites()
    {
        _prefix = $"FT{Guid.NewGuid():N}"[..10];

        _category = new Category { Name = $"{_prefix}-Category" };
        await TestApp.AddAsync(_category);

        _schoolClass = new SchoolClass
        {
            Name = $"{_prefix}-Class",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 6, 1)
        };
        await TestApp.AddAsync(_schoolClass);
    }

    private async Task<Item> SeedItemAsync(string name)
    {
        var item = new Item { Name = $"{_prefix}-{name}", Unit = "unit", CategoryId = _category.Id };
        await TestApp.AddAsync(item);
        return item;
    }

    private async Task<Location> SeedLocationAsync(string name)
    {
        var location = new Location { Name = $"{_prefix}-{name}", Type = "Kitchen" };
        await TestApp.AddAsync(location);
        return location;
    }

    private async Task<GoodsReceipt> SeedGoodsReceiptAsync(string note)
    {
        // Only the FK scalar is set (not the Class navigation): _schoolClass was persisted via
        // a previous TestApp.AddAsync call using a different DbContext scope, so it's a detached
        // instance here. Assigning it as a navigation would make EF's Add() cascade Added state
        // onto it too, causing SaveChangesAsync to try to re-insert an already-existing row.
        var receipt = new GoodsReceipt { ClassId = _schoolClass.Id, Class = null!, Note = $"{_prefix}-{note}" };
        await TestApp.AddAsync(receipt);
        return receipt;
    }

    private async Task<StockBatch> SeedBatchAsync(
        Item item, Location location, GoodsReceipt receipt, int quantity, decimal unitPrice,
        DateOnly? receivedDate = null)
    {
        // Only FK scalars are set here (Item/Location/ReceivedClass/GoodsReceipt navigations are
        // left at their null! default) - item/location/receipt were persisted via previous
        // TestApp.AddAsync calls using different DbContext scopes, so they're detached here.
        // Assigning them as navigations would make EF's Add() cascade Added state onto them too,
        // causing SaveChangesAsync to try to re-insert already-existing rows.
        var batch = new StockBatch
        {
            ItemId = item.Id,
            LocationId = location.Id,
            ReceivedClassId = _schoolClass.Id,
            GoodsReceiptId = receipt.Id,
            Quantity = quantity,
            UnitPrice = unitPrice,
            ReceivedDate = receivedDate ?? new DateOnly(2024, 1, 1)
        };

        await TestApp.AddAsync(batch);
        return batch;
    }

    private static GetAllStockBatchesQuery Query(
        string searchTerm,
        int pageSize = PaginationConstants.DEFAULT_PAGE_SIZE,
        string? cursor = null,
        List<PaginationSort>? sort = null,
        List<ColumnFilter>? filters = null) =>
        new()
        {
            SearchTerm = searchTerm,
            PageSize = pageSize,
            Cursor = cursor,
            Sort = sort ?? [],
            Filters = filters ?? []
        };

    [Test]
    public async Task Handle_ReturnsOnlySeededBatchesMatchingSearchTerm()
    {
        var item = await SeedItemAsync("Flour");
        var location = await SeedLocationAsync("MainKitchen");
        var receipt = await SeedGoodsReceiptAsync("Receipt");
        await SeedBatchAsync(item, location, receipt, 10, 2.5m);

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Select(b => b.ItemName).ShouldAllBe(name => name.StartsWith(_prefix));
    }

    [Test]
    public async Task Handle_WithNoMatches_ReturnsEmptyPage()
    {
        var result = await TestApp.SendAsync(Query($"{_prefix}-does-not-exist"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ReturnsDtoWithItemNameLocationNameAndLineTotal()
    {
        var item = await SeedItemAsync("Sugar");
        var location = await SeedLocationAsync("StorageRoom");
        var receipt = await SeedGoodsReceiptAsync("Receipt");
        var batch = await SeedBatchAsync(item, location, receipt, 4, 3.25m);

        var result = await TestApp.SendAsync(Query(_prefix));

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.Data.Single();
        dto.Id.ShouldBe(batch.Id);
        dto.ItemId.ShouldBe(item.Id);
        dto.ItemName.ShouldBe(item.Name);
        dto.LocationId.ShouldBe(location.Id);
        dto.LocationName.ShouldBe(location.Name);
        dto.GoodsReceiptId.ShouldBe(receipt.Id);
        dto.Quantity.ShouldBe(4);
        dto.UnitPrice.ShouldBe(3.25m);
        dto.LineTotal.ShouldBe(13m);
    }

    [Test]
    public async Task Handle_WithGoodsReceiptIdFilter_ReturnsOnlyBatchesFromThatReceipt()
    {
        var item = await SeedItemAsync("Rice");
        var location = await SeedLocationAsync("MainKitchen");
        var receiptA = await SeedGoodsReceiptAsync("ReceiptA");
        var receiptB = await SeedGoodsReceiptAsync("ReceiptB");
        await SeedBatchAsync(item, location, receiptA, 5, 1m);
        await SeedBatchAsync(item, location, receiptB, 7, 1m);

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("goodsReceiptId", FilterOperator.Equals, receiptA.Id)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().GoodsReceiptId.ShouldBe(receiptA.Id);
    }

    [Test]
    public async Task Handle_WithItemIdFilter_ReturnsOnlyMatchingBatches()
    {
        var itemA = await SeedItemAsync("Oats");
        var itemB = await SeedItemAsync("Milk");
        var location = await SeedLocationAsync("MainKitchen");
        var receipt = await SeedGoodsReceiptAsync("Receipt");
        await SeedBatchAsync(itemA, location, receipt, 1, 1m);
        await SeedBatchAsync(itemB, location, receipt, 2, 1m);

        var result = await TestApp.SendAsync(Query(
            _prefix,
            filters: [new ColumnFilter("itemId", FilterOperator.Equals, itemA.Id)]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().ItemId.ShouldBe(itemA.Id);
    }

    [Test]
    public async Task Handle_WithReceivedDateSortAscending_ReturnsItemsInReceivedDateOrder()
    {
        var item = await SeedItemAsync("Pasta");
        var location = await SeedLocationAsync("MainKitchen");
        var receipt = await SeedGoodsReceiptAsync("Receipt");
        await SeedBatchAsync(item, location, receipt, 1, 1m, new DateOnly(2024, 3, 1));
        await SeedBatchAsync(item, location, receipt, 2, 1m, new DateOnly(2024, 1, 1));
        await SeedBatchAsync(item, location, receipt, 3, 1m, new DateOnly(2024, 2, 1));

        var result = await TestApp.SendAsync(Query(
            _prefix,
            sort: [new PaginationSort { Key = "receivedDate", Value = "ascend" }]));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(b => b.Quantity).ShouldBe([2, 3, 1]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllSeededBatchesExactlyOnce()
    {
        var item = await SeedItemAsync("Beans");
        var location = await SeedLocationAsync("MainKitchen");
        var receipt = await SeedGoodsReceiptAsync("Receipt");

        for (var i = 0; i < 7; i++)
            await SeedBatchAsync(item, location, receipt, i, 1m);

        var collected = new List<StockBatchListItemDto>();
        string? cursor = null;
        var safety = 0;

        while (true)
        {
            safety++;
            safety.ShouldBeLessThan(20);

            var result = await TestApp.SendAsync(Query(
                _prefix,
                pageSize: 3,
                cursor: cursor,
                sort: [new PaginationSort { Key = "id", Value = "ascend" }]));

            result.IsSuccess.ShouldBeTrue();
            collected.AddRange(result.Value.Data);

            if (!result.Value.HasNextPage)
                break;

            cursor = result.Value.NextCursor;
        }

        collected.Select(b => b.Id).Distinct().Count().ShouldBe(7);
        collected.Select(b => b.Quantity).ShouldBe(Enumerable.Range(0, 7));
    }

    [Test]
    public async Task Handle_WithInvalidPageSize_ThrowsValidationException()
    {
        var item = await SeedItemAsync("Salt");
        var location = await SeedLocationAsync("MainKitchen");
        var receipt = await SeedGoodsReceiptAsync("Receipt");
        await SeedBatchAsync(item, location, receipt, 1, 1m);

        var act = async () => await TestApp.SendAsync(Query(_prefix, pageSize: 0));

        var exception = await act.ShouldThrowAsync<ValidationException>();
        exception.Errors.ShouldContainKey(nameof(GetAllStockBatchesQuery.PageSize));
    }

    [Test]
    public async Task Handle_WithMalformedCursor_ThrowsValidationException()
    {
        var act = async () => await TestApp.SendAsync(Query(_prefix, cursor: "not-a-valid-cursor"));

        await act.ShouldThrowAsync<ValidationException>();
    }

    [Test]
    public async Task Handle_WithCursorFromDifferentSort_ThrowsValidationException()
    {
        var item = await SeedItemAsync("Pepper");
        var location = await SeedLocationAsync("MainKitchen");
        var receipt = await SeedGoodsReceiptAsync("Receipt");
        await SeedBatchAsync(item, location, receipt, 1, 1m, new DateOnly(2024, 1, 1));
        await SeedBatchAsync(item, location, receipt, 2, 1m, new DateOnly(2024, 2, 1));
        await SeedBatchAsync(item, location, receipt, 3, 1m, new DateOnly(2024, 3, 1));

        var firstPage = await TestApp.SendAsync(Query(
            _prefix,
            pageSize: 1,
            sort: [new PaginationSort { Key = "receivedDate", Value = "ascend" }]));

        firstPage.Value.HasNextPage.ShouldBeTrue();

        var act = async () => await TestApp.SendAsync(Query(
            _prefix,
            cursor: firstPage.Value.NextCursor,
            sort: [new PaginationSort { Key = "receivedDate", Value = "descend" }]));

        await act.ShouldThrowAsync<ValidationException>();
    }
}
