using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportById;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Queries.GetGoodsReceiptImportById;

public class GetGoodsReceiptImportByIdHandlerTests
{
    private static async Task<(GoodsReceiptImportReviewTestDbContext Context, SchoolClass Class, Category Category)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptImportReviewTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptImportReviewTestDbContext(options);

        var category = new Category { Name = "Pantry" };
        context.Categories.Add(category);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, schoolClass, category);
    }

    private static async Task<GoodsReceiptImport> AddImportAsync(
        GoodsReceiptImportReviewTestDbContext context, Guid classId, GoodsReceiptExtractionResult extraction)
    {
        var import = GoodsReceiptImport.Create(classId, Guid.NewGuid(), Guid.NewGuid(), "blob/path.pdf");
        import.ApplyExtractionResult(extraction.ToJson());
        context.GoodsReceiptImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);
        return import;
    }

    [Test]
    public async Task Handle_WhenSkuMatchesActiveItem_PopulatesMatchedItem()
    {
        var (context, schoolClass, category) = await CreateContextAsync();
        await using var _ = context;

        var item = new Item { Sku = "SKU-1", Name = "Rice", Unit = "kg", IsPerishable = false, Category = category };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var extraction = new GoodsReceiptExtractionResult
        {
            LineItems = [new GoodsReceiptLineItem { ProductCode = "sku-1", Name = "Rice bag", Quantity = 10, UnitPrice = 2.5m }]
        };
        var import = await AddImportAsync(context, schoolClass.Id, extraction);

        var handler = new GetGoodsReceiptImportByIdHandler(context);
        var result = await handler.Handle(new GetGoodsReceiptImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Lines.Count.ShouldBe(1);
        var line = result.Value.Lines[0];
        line.MatchedItem.ShouldNotBeNull();
        line.MatchedItem!.Id.ShouldBe(item.Id);
        line.MatchedItem.Name.ShouldBe("Rice");
        line.MatchedItem.CategoryName.ShouldBe("Pantry");
    }

    [Test]
    public async Task Handle_WhenProductCodeHasNoMatch_LeavesMatchedItemNull()
    {
        var (context, schoolClass, _) = await CreateContextAsync();
        await using var _ = context;

        var extraction = new GoodsReceiptExtractionResult
        {
            LineItems = [new GoodsReceiptLineItem { ProductCode = "UNKNOWN", Name = "Mystery", Quantity = 3 }]
        };
        var import = await AddImportAsync(context, schoolClass.Id, extraction);

        var handler = new GetGoodsReceiptImportByIdHandler(context);
        var result = await handler.Handle(new GetGoodsReceiptImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.Value.Lines[0].MatchedItem.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenProductCodeIsNull_LeavesMatchedItemNull()
    {
        var (context, schoolClass, _) = await CreateContextAsync();
        await using var _ = context;

        var extraction = new GoodsReceiptExtractionResult
        {
            LineItems = [new GoodsReceiptLineItem { ProductCode = null, Name = "No code", Quantity = 1 }]
        };
        var import = await AddImportAsync(context, schoolClass.Id, extraction);

        var handler = new GetGoodsReceiptImportByIdHandler(context);
        var result = await handler.Handle(new GetGoodsReceiptImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.Value.Lines[0].MatchedItem.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenMatchingItemIsInactive_LeavesMatchedItemNull()
    {
        var (context, schoolClass, category) = await CreateContextAsync();
        await using var _ = context;

        var item = new Item { Sku = "SKU-2", Name = "Old", Unit = "unit", IsActive = false, Category = category };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var extraction = new GoodsReceiptExtractionResult
        {
            LineItems = [new GoodsReceiptLineItem { ProductCode = "SKU-2", Name = "Old thing", Quantity = 1 }]
        };
        var import = await AddImportAsync(context, schoolClass.Id, extraction);

        var handler = new GetGoodsReceiptImportByIdHandler(context);
        var result = await handler.Handle(new GetGoodsReceiptImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.Value.Lines[0].MatchedItem.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFound()
    {
        var (context, _, _) = await CreateContextAsync();
        await using var _ = context;

        var handler = new GetGoodsReceiptImportByIdHandler(context);
        var result = await handler.Handle(new GetGoodsReceiptImportByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }
}
