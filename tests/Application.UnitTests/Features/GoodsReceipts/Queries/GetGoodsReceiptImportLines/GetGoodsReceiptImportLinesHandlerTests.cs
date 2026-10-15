using Microsoft.EntityFrameworkCore;
using FluentResults;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Domain.Entities.Categories;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;
using skestock.Domain.Entities.GoodsReceipts;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;
using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Stock;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Queries.GetGoodsReceiptImportLines;

public class GetGoodsReceiptImportLinesHandlerTests
{
    [Test]
    public async Task MissingReceipt_ReturnsNotFound()
    {
        await using var db = NewContext();
        var result = await new GetGoodsReceiptImportLinesHandler(db).Handle(
            new GetGoodsReceiptImportLinesQuery { Id = Guid.NewGuid() }, CancellationToken.None);
        result.IsFailed.ShouldBeTrue();
        result.Errors.Single().GetType().Name.ShouldBe("GoodsReceiptNotFound");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ReceiptWithoutSavedLines_ReturnsEmptyArray(bool hasLegacyImport)
    {
        await using var db = NewContext();
        var receipt = AddReceipt(db);
        if (hasLegacyImport) AddImport(db, receipt);
        await db.SaveChangesAsync();
        var result = await Read(db, receipt.Id);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Test]
    public async Task SavedLines_AreOrderedAndIsolatedFromOtherReceiptsAndUnconfirmedImports()
    {
        await using var db = NewContext();
        var receipt = AddReceipt(db);
        var import = AddImport(db, receipt);
        var date = new DateTimeOffset(2026, 10, 14, 8, 0, 0, TimeSpan.Zero);
        var first = AddLine(db, import, "Original source", 6, 2.5m, date);
        first.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var split = AddLine(db, import, "Original source", 4, 2.5m, date);
        split.Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var last = AddLine(db, import, "Later", 3, 1m, date.AddMinutes(1));
        var location = new Location { Name = "Warehouse", Type = "StorageRoom" };
        first.Location = location;
        first.LocationId = location.Id;
        first.ExpiryDate = new DateOnly(2027, 1, 1);
        var other = AddImport(db, AddReceipt(db));
        AddLine(db, other, "Other receipt", 99, 99m, date);
        var pending = AddImport(db, receipt);
        pending.Status = GoodsReceiptImportStatus.PendingReview;
        AddLine(db, pending, "Not confirmed", 99, 99m, date);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var result = await Read(db, receipt.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Select(l => l.Id).ShouldBe([first.Id, split.Id, last.Id]);
        var row = result.Value[0];
        row.ImportId.ShouldBe(import.Id);
        row.RawItemText.ShouldBe("Original source");
        row.Quantity.ShouldBe(6);
        row.UnitPrice.ShouldBe(2.5m);
        row.LineTotal.ShouldBe(15m);
        row.ExpiryDate.ShouldBe(new DateOnly(2027, 1, 1));
        row.LocationId.ShouldBe(location.Id);
        row.LocationName.ShouldBe("Warehouse");
        row.MatchedItemId.ShouldBeNull();
        result.Value[1].LocationId.ShouldBeNull();
        result.Value[1].LocationName.ShouldBeNull();
        result.Value[1].ExpiryDate.ShouldBeNull();
    }

    [Test]
    public async Task StockAndCatalogChanges_DoNotChangeSavedScalarsOrText()
    {
        await using var db = NewContext();
        var receipt = AddReceipt(db);
        var import = AddImport(db, receipt);
        var item = new Item { Name = "Catalog name", Category = new Category { Name = "Supplies" } };
        var location = new Location { Name = "Warehouse", Type = "StorageRoom" };
        var line = AddLine(db, import, "Invoice name", 8, 3.25m, DateTimeOffset.UtcNow);
        line.MatchedItem = item;
        line.MatchedItemId = item.Id;
        line.Location = location;
        line.LocationId = location.Id;
        line.ExpiryDate = new DateOnly(2027, 2, 1);
        var batch = new StockBatch
        {
            Item = item, Location = location, GoodsReceipt = receipt,
            ReceivedClass = receipt.Class, Quantity = 8, UnitPrice = 3.25m,
            ExpiryDate = line.ExpiryDate
        };
        db.StockBatches.Add(batch);
        await db.SaveChangesAsync();
        batch.Quantity = 1;
        batch.UnitPrice = 99m;
        batch.ExpiryDate = null;
        item.Name = "Renamed catalog item";
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var row = (await Read(db, receipt.Id)).Value.Single();
        row.Id.ShouldBe(line.Id);
        row.MatchedItemId.ShouldBe(item.Id);
        row.RawItemText.ShouldBe("Invoice name");
        row.Quantity.ShouldBe(8);
        row.UnitPrice.ShouldBe(3.25m);
        row.LineTotal.ShouldBe(26m);
        row.ExpiryDate.ShouldBe(new DateOnly(2027, 2, 1));
    }

    private static GoodsReceiptImportLinesTestDbContext NewContext() => new(
        new DbContextOptionsBuilder<GoodsReceiptImportLinesTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static GoodsReceipt AddReceipt(GoodsReceiptImportLinesTestDbContext db)
    {
        var schoolClass = new SchoolClass { Name = "Class" };
        var receipt = new GoodsReceipt { Class = schoolClass, ClassId = schoolClass.Id, Note = "Receipt" };
        db.GoodsReceipts.Add(receipt);
        return receipt;
    }

    private static GoodsReceiptImport AddImport(GoodsReceiptImportLinesTestDbContext db, GoodsReceipt receipt)
    {
        var import = new GoodsReceiptImport
        {
            Class = receipt.Class, ClassId = receipt.ClassId, BlobPath = "receipt.pdf",
            Status = GoodsReceiptImportStatus.Confirmed, ResultingGoodsReceiptId = receipt.Id
        };
        db.GoodsReceiptImports.Add(import);
        return import;
    }

    private static GoodsReceiptImportLine AddLine(GoodsReceiptImportLinesTestDbContext db,
        GoodsReceiptImport import, string text, int quantity, decimal price, DateTimeOffset created)
    {
        var line = new GoodsReceiptImportLine
        {
            Import = import, ImportId = import.Id, RawItemText = text,
            Quantity = quantity, UnitPrice = price, CreatedDate = created
        };
        db.GoodsReceiptImportLines.Add(line);
        return line;
    }

    private static ValueTask<Result<List<GoodsReceiptImportLineDto>>>
        Read(GoodsReceiptImportLinesTestDbContext db, Guid id) =>
        new GetGoodsReceiptImportLinesHandler(db).Handle(new GetGoodsReceiptImportLinesQuery { Id = id }, CancellationToken.None);
}
