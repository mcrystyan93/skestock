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
        milk.MatchedCategory.ShouldNotBeNull();
        milk.MatchedCategory!.Id.ShouldBe(category.Id);
        milk.MatchedCategory.Name.ShouldBe("Dairy");
        milk.MatchedItem.ShouldNotBeNull();
        milk.MatchedItem!.Id.ShouldBe(context.Items.Single().Id);
        milk.MatchedItem.CategoryId.ShouldBe(category.Id);

        var bread = result.Value.Suggestions.Single(s => s.Sku == "SKU-2");
        bread.ItemAlreadyExists.ShouldBeFalse();
        bread.CategoryAlreadyExists.ShouldBeFalse();
        bread.MatchedCategory.ShouldBeNull();
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

    [Test]
    public async Task Handle_MatchesItemByUniqueNameAndCategoryWhenSkuDoesNotMatch()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Dairy" };
        var item = new Item
        {
            Name = "Milk",
            Unit = "L",
            Category = category,
            CategoryId = category.Id
        };
        context.Categories.Add(category);
        context.Items.Add(item);

        var import = ItemImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/items.pdf");
        import.ApplyExtractionResult(JsonSerializer.Serialize(new ItemExtractionResult
        {
            Items = [new ExtractedItem { Sku = "new-sku", Name = " milk ", CategoryName = " dairy " }]
        }));
        context.ItemImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetItemImportByIdHandler(context)
            .Handle(new GetItemImportByIdQuery { Id = import.Id }, CancellationToken.None);

        var suggestion = result.Value.Suggestions.Single();
        suggestion.MatchedItem.ShouldNotBeNull();
        suggestion.MatchedItem!.Id.ShouldBe(item.Id);
    }

    [Test]
    public async Task Handle_LeavesAmbiguousNameAndCategoryMatchUnresolved()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Dairy" };
        context.Categories.Add(category);
        context.Items.AddRange(
            new Item { Name = "Milk", Category = category, CategoryId = category.Id },
            new Item { Name = "Milk", Category = category, CategoryId = category.Id });

        var import = ItemImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/items.pdf");
        import.ApplyExtractionResult(JsonSerializer.Serialize(new ItemExtractionResult
        {
            Items = [new ExtractedItem { Name = "Milk", CategoryName = "Dairy" }]
        }));
        context.ItemImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new GetItemImportByIdHandler(context)
            .Handle(new GetItemImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.Value.Suggestions.Single().MatchedItem.ShouldBeNull();
    }
}
