using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.ConfirmItemImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Items.Commands.ConfirmItemImport;

public class ConfirmItemImportCommandHandlerTests
{
    private static ConfirmItemImportTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConfirmItemImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConfirmItemImportTestDbContext(options);
    }

    private static async Task<ItemImport> AddPendingImportAsync(ConfirmItemImportTestDbContext context)
    {
        var import = ItemImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/items.pdf");
        import.Status = ItemImportStatus.PendingReview;
        context.ItemImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);
        return import;
    }

    private static async Task<(Category Category, Item Item)> AddItemAsync(
        ConfirmItemImportTestDbContext context,
        string categoryName = "Dairy",
        string itemName = "Milk")
    {
        var category = new Category { Name = categoryName };
        var item = new Item
        {
            Sku = "SKU-1",
            Name = itemName,
            Unit = "L",
            Category = category,
            CategoryId = category.Id
        };
        context.Categories.Add(category);
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);
        return (category, item);
    }

    [Test]
    public async Task Handle_WithSelectedItem_ConfirmsWithoutCreatingOrMutatingCatalogRows()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var (_, item) = await AddItemAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);

        var result = await handler.Handle(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items =
            [
                new ConfirmItemImportItem
                {
                    ItemId = item.Id,
                    Sku = "different",
                    Name = "Edited suggestion"
                }
            ]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().Id.ShouldBe(item.Id);
        result.Value.Items.Single().Name.ShouldBe(item.Name);
        result.Value.Items.Single().CategoryName.ShouldBe("Dairy");
        result.Value.Items.Single().Created.ShouldBeFalse();
        result.Value.Items.Single().CategoryCreated.ShouldBeFalse();
        import.ConfirmationResultJson.ShouldNotBeNull();
        import.ConfirmationResultJson.ShouldNotContain("CategoryId");
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(1);
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Handle_WhenItemDoesNotExist_ReturnsMissingItemIds()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var missingId = Guid.NewGuid();

        var result = await new ConfirmItemImportCommandHandler(context).Handle(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items = [new ConfirmItemImportItem { ItemId = missingId, Name = "Milk" }]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is ItemImportErrors.ItemsNotFound);
    }

    [Test]
    public async Task Handle_WhenItemIsSelectedTwice_RejectsDuplicateSelection()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var (_, item) = await AddItemAsync(context);

        var result = await new ConfirmItemImportCommandHandler(context).Handle(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items =
            [
                new ConfirmItemImportItem { ItemId = item.Id, Name = "Milk" },
                new ConfirmItemImportItem { ItemId = item.Id, Name = "Milk duplicate" }
            ]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is ItemImportErrors.DuplicateItems);
    }

    [Test]
    public async Task Handle_WithSelectedItem_DerivesCategoryFromCatalog()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var (_, item) = await AddItemAsync(context);

        var result = await new ConfirmItemImportCommandHandler(context).Handle(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items = [new ConfirmItemImportItem { ItemId = item.Id, Name = "Milk" }]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().CategoryName.ShouldBe("Dairy");
    }

    [Test]
    public async Task Handle_RepeatedConfirmation_ReturnsStoredResult()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var (_, item) = await AddItemAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);
        var command = new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items = [new ConfirmItemImportItem { ItemId = item.Id, Name = item.Name }]
        };

        var first = await handler.Handle(command, CancellationToken.None);
        var second = await handler.Handle(command, CancellationToken.None);

        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        second.Value.Items.Select(i => i.Id).ShouldBe(first.Value.Items.Select(i => i.Id));
    }

    [Test]
    public async Task Handle_RepeatedConfirmation_ReadsLegacyStoredCategoryId()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var (_, item) = await AddItemAsync(context);

        import.MarkAsConfirmed(System.Text.Json.JsonSerializer.Serialize(new
        {
            ImportId = import.Id,
            Status = ItemImportStatus.Confirmed,
            Items = new[]
            {
                new
                {
                    Id = item.Id,
                    Sku = item.Sku,
                    Name = item.Name,
                    CategoryId = item.CategoryId,
                    CategoryName = "Dairy",
                    Created = false,
                    CategoryCreated = false
                }
            }
        }));
        await context.SaveChangesAsync(CancellationToken.None);

        var result = await new ConfirmItemImportCommandHandler(context).Handle(new ConfirmItemImportCommand
        {
            ImportId = import.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().CategoryName.ShouldBe("Dairy");
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();

        var result = await new ConfirmItemImportCommandHandler(context).Handle(new ConfirmItemImportCommand
        {
            ImportId = Guid.NewGuid(),
            Items = []
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(error => error is ItemImportErrors.ItemImportNotFound);
    }

    [Test]
    public async Task Handle_WhenReviewedRowHasNoCategory_AllowsConfirmation()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var (_, item) = await AddItemAsync(context);

        var result = await new ConfirmItemImportCommandHandler(context).Handle(new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items = [new ConfirmItemImportItem { ItemId = item.Id, Name = "Milk" }]
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single().CategoryName.ShouldBe("Dairy");
    }
}
