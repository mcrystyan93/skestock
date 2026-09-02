using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Filtering;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Models;
using skestock.Application.Features.StockBatches.Models;
using skestock.Application.Features.StockBatches.Queries.GetAllStockBatches;
using skestock.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.StockBatches.Queries.GetAllStockBatches;

public class GetAllStockBatchesHandlerTests
{
    private static async Task<(StockBatchTestDbContext Context, Item Item, Location Location, SchoolClass Class, GoodsReceipt Receipt)> SeedPrerequisitesAsync(
        StockBatchTestDbContext? context = null)
    {
        context ??= NewContext();

        var category = new Category { Name = "Category" };
        context.Categories.Add(category);

        var item = new Item { Name = "Flour", Category = category };
        context.Items.Add(item);

        var location = new Location { Name = "Main Kitchen", Type = "Kitchen" };
        context.Locations.Add(location);

        var schoolClass = new SchoolClass
        {
            Name = "Cycle 1",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 6, 1)
        };
        context.SchoolClasses.Add(schoolClass);

        var receipt = new GoodsReceipt { ClassId = schoolClass.Id, Class = schoolClass, Note = "Receipt" };
        context.GoodsReceipts.Add(receipt);

        await context.SaveChangesAsync(CancellationToken.None);
        return (context, item, location, schoolClass, receipt);
    }

    private static StockBatchTestDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<StockBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new StockBatchTestDbContext(options);
    }

    private static StockBatch MakeBatch(
        Item item, Location location, SchoolClass schoolClass, GoodsReceipt? receipt,
        int quantity, decimal unitPrice, DateTimeOffset createdDate,
        DateOnly? receivedDate = null, DateOnly? expiryDate = null) => new()
    {
        Item = item,
        Location = location,
        ReceivedClass = schoolClass,
        GoodsReceipt = receipt,
        Quantity = quantity,
        UnitPrice = unitPrice,
        ReceivedDate = receivedDate ?? DateOnly.FromDateTime(createdDate.Date),
        ExpiryDate = expiryDate,
        CreatedDate = createdDate,
        LastModifiedDate = createdDate
    };

    private static GetAllStockBatchesHandler CreateHandler(IApplicationDbContext context) => new(context);

    // ---- Basic pagination -------------------------------------------------

    [Test]
    public async Task Handle_WithNoCursor_ReturnsFirstPageOrderedByDefaultSort()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 5; i++)
            context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 10 + i, 1.5m, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllStockBatchesQuery { PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(5);
        page.HasNextPage.ShouldBeFalse();
        page.NextCursor.ShouldNotBeNullOrWhiteSpace();

        // Default sort is CreatedDate desc, Id desc -> most-recently-created batch first.
        page.Data.Select(b => b.Quantity).ShouldBe([14, 13, 12, 11, 10]);
    }

    [Test]
    public async Task Handle_WithPageSizeSmallerThanTotal_SetsHasNextPageAndReturnsCursor()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 5; i++)
            context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 10 + i, 1.5m, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllStockBatchesQuery { PageSize = 2 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var page = result.Value;
        page.Data.Count().ShouldBe(2);
        page.HasNextPage.ShouldBeTrue();
        page.NextCursor.ShouldNotBeNullOrWhiteSpace();
        page.Data.Select(b => b.Quantity).ShouldBe([14, 13]);
    }

    [Test]
    public async Task Handle_PagingThroughCursors_ReturnsAllItemsExactlyOnceInOrder()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 11; i++)
            context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, i, 1m, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var collected = new List<StockBatchListItemDto>();
        string? cursor = null;
        var safetyCounter = 0;

        while (true)
        {
            safetyCounter++.ShouldBeLessThan(20);

            var result = await handler.Handle(
                new GetAllStockBatchesQuery { PageSize = 3, Cursor = cursor },
                CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            var page = result.Value;
            collected.AddRange(page.Data);

            if (!page.HasNextPage)
                break;

            page.NextCursor.ShouldNotBeNullOrWhiteSpace();
            cursor = page.NextCursor;
        }

        collected.Select(b => b.Id).Distinct().Count().ShouldBe(11);
        collected.Select(b => b.Quantity).ShouldBe(Enumerable.Range(0, 11).Reverse());
    }

    [Test]
    public async Task Handle_WithEmptyDataSet_ReturnsEmptyPageWithNoCursor()
    {
        var context = NewContext();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllStockBatchesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
        result.Value.NextCursor.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithPageSizeAboveMaximum_ClampsToDefaultPageSize()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        for (var i = 0; i < 60; i++)
            context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, i, 1m, baseline.AddMinutes(i)));

        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllStockBatchesQuery { PageSize = 1000 }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(PaginationConstants.DEFAULT_PAGE_SIZE);
        result.Value.HasNextPage.ShouldBeTrue();
    }

    // ---- DTO shape -------------------------------------------------------

    [Test]
    public async Task Handle_ReturnsDtoWithItemNameLocationNameAndLineTotal()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 4, 2.5m, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllStockBatchesQuery(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var dto = result.Value.Data.Single();
        dto.ItemName.ShouldBe("Flour");
        dto.LocationName.ShouldBe("Main Kitchen");
        dto.Quantity.ShouldBe(4);
        dto.UnitPrice.ShouldBe(2.5m);
        dto.LineTotal.ShouldBe(10m);
        dto.GoodsReceiptId.ShouldBe(receipt.Id);
    }

    // ---- Filtering ------------------------------------------------------------

    [Test]
    public async Task Handle_WithGoodsReceiptIdFilter_ReturnsOnlyBatchesFromThatReceipt()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var otherReceipt = new GoodsReceipt { ClassId = schoolClass.Id, Class = schoolClass, Note = "Other" };
        context.GoodsReceipts.Add(otherReceipt);

        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 1, 1m, DateTimeOffset.UtcNow));
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, otherReceipt, 2, 1m, DateTimeOffset.UtcNow.AddMinutes(1)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllStockBatchesQuery
        {
            Filters = [new ColumnFilter("goodsReceiptId", FilterOperator.Equals, receipt.Id)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().GoodsReceiptId.ShouldBe(receipt.Id);
    }

    [Test]
    public async Task Handle_WithItemIdFilter_ReturnsOnlyMatchingBatches()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var category = context.Categories.First();
        var otherItem = new Item { Name = "Sugar", Category = category };
        context.Items.Add(otherItem);

        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 1, 1m, DateTimeOffset.UtcNow));
        context.StockBatches.Add(MakeBatch(otherItem, location, schoolClass, receipt, 2, 1m, DateTimeOffset.UtcNow.AddMinutes(1)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllStockBatchesQuery
        {
            Filters = [new ColumnFilter("itemId", FilterOperator.Equals, item.Id)]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().ItemName.ShouldBe("Flour");
    }

    [Test]
    public async Task Handle_WithColumnFilterNotMatchingAnyRow_ReturnsEmptyPage()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 1, 1m, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllStockBatchesQuery
        {
            Filters = [new ColumnFilter("goodsReceiptId", FilterOperator.Equals, Guid.NewGuid())]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithSearchTerm_FiltersByItemOrLocationName()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var otherLocation = new Location { Name = "Storage Room", Type = "StorageRoom" };
        context.Locations.Add(otherLocation);

        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 1, 1m, DateTimeOffset.UtcNow)); // Flour / Main Kitchen
        context.StockBatches.Add(MakeBatch(item, otherLocation, schoolClass, receipt, 2, 1m, DateTimeOffset.UtcNow.AddMinutes(1))); // Flour / Storage Room
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(new GetAllStockBatchesQuery { SearchTerm = "Storage" }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
        result.Value.Data.Single().LocationName.ShouldBe("Storage Room");
    }

    // ---- Sorting ------------------------------------------------------------

    [Test]
    public async Task Handle_WithReceivedDateSortAscending_ReturnsItemsInReceivedDateOrder()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 1, 1m, DateTimeOffset.UtcNow, new DateOnly(2024, 3, 1)));
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 2, 1m, DateTimeOffset.UtcNow.AddMinutes(1), new DateOnly(2024, 1, 1)));
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 3, 1m, DateTimeOffset.UtcNow.AddMinutes(2), new DateOnly(2024, 2, 1)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllStockBatchesQuery
        {
            Sort = [new PaginationSort { Key = "receivedDate", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Select(b => b.Quantity).ShouldBe([2, 3, 1]);
        result.Value.Sort.ShouldContain(s => s.Key == "receivedDate" && s.Value == "ascend");
    }

    [Test]
    public async Task Handle_WithUnknownSortKey_FallsBackToDefaultSort()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        var baseline = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 3; i++)
            context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, i, 1m, baseline.AddMinutes(i)));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var query = new GetAllStockBatchesQuery
        {
            Sort = [new PaginationSort { Key = "totallyUnknownKey", Value = "ascend" }]
        };
        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        // Falls back to default sort (CreatedDate desc, Id desc).
        result.Value.Data.Select(b => b.Quantity).ShouldBe([2, 1, 0]);
    }

    // ---- Cursor edge cases ------------------------------------------------

    [Test]
    public async Task Handle_WithMalformedCursor_IsIgnoredAndReturnsFirstPage()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 1, 1m, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllStockBatchesQuery { Cursor = "not-a-valid-cursor-token" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
    }

    [Test]
    public async Task Handle_WithEmptyCursor_ReturnsFirstPage()
    {
        var (context, item, location, schoolClass, receipt) = await SeedPrerequisitesAsync();
        context.StockBatches.Add(MakeBatch(item, location, schoolClass, receipt, 1, 1m, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync(CancellationToken.None);
        var handler = CreateHandler(context);

        var result = await handler.Handle(
            new GetAllStockBatchesQuery { Cursor = "" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Data.Count().ShouldBe(1);
    }
}
