using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Items.Queries.GetItemImportById;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.Queries.GetItemImportById;

public class GetItemImportByIdHandlerTests
{
    private static ItemImportReviewTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ItemImportReviewTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ItemImportReviewTestDbContext(options);
    }

    [Test]
    public async Task Handle_ReturnsSuggestionsFlaggingExistingSkuAndCategoryCaseInsensitively()
    {
        await using var context = CreateContext();

        var category = new Category { Name = "Dairy" };
        context.Categories.Add(category);
        context.Items.Add(new Item { Sku = "SKU-1", Name = "Milk 1L", Unit = "L", Category = category, CategoryId = category.Id });

        var import = ItemImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/items.pdf");
        var extraction = new ItemExtractionResult
        {
            Items =
            [
                new ExtractedItem { Sku = "sku-1", Name = "Milk", CategoryName = "dairy", Unit = "L" },
                new ExtractedItem { Sku = "SKU-2", Name = "Bread", CategoryName = "Bakery", Unit = "unit" }
            ]
        };
        import.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
        context.ItemImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetItemImportByIdHandler(context);

        var result = await handler.Handle(new GetItemImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ItemImportStatus.PendingReview);
        result.Value.Suggestions.Count.ShouldBe(2);

        var milk = result.Value.Suggestions.Single(s => s.Sku == "sku-1");
        milk.ItemAlreadyExists.ShouldBeTrue();
        milk.CategoryAlreadyExists.ShouldBeTrue();

        var bread = result.Value.Suggestions.Single(s => s.Sku == "SKU-2");
        bread.ItemAlreadyExists.ShouldBeFalse();
        bread.CategoryAlreadyExists.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var handler = new GetItemImportByIdHandler(context);

        var result = await handler.Handle(new GetItemImportByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenNoExtractionYet_ReturnsEmptySuggestions()
    {
        await using var context = CreateContext();

        var import = ItemImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/items.pdf");
        context.ItemImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetItemImportByIdHandler(context);

        var result = await handler.Handle(new GetItemImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ItemImportStatus.Processing);
        result.Value.Suggestions.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_DeduplicatesSuggestionsBySkuCaseInsensitively()
    {
        await using var context = CreateContext();

        var import = ItemImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/items.pdf");
        var extraction = new ItemExtractionResult
        {
            Items =
            [
                new ExtractedItem { Sku = "SKU-1", Name = "Milk", CategoryName = "Dairy" },
                new ExtractedItem { Sku = "sku-1", Name = "Milk (dup)", CategoryName = "Dairy" }
            ]
        };
        import.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
        context.ItemImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetItemImportByIdHandler(context);

        var result = await handler.Handle(new GetItemImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Suggestions.Count.ShouldBe(1);
    }
}
