using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
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

    [Test]
    public async Task Handle_WithNewItems_CreatesReviewedItemsAndCategoriesAndMarksConfirmed()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);

        var command = new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items =
            [
                new ConfirmItemImportItem { Sku = "SKU-1", Name = "Milk", CategoryName = "Dairy", Unit = "L" },
                new ConfirmItemImportItem { Sku = "SKU-2", Name = "Bread", CategoryName = "Bakery", Unit = "unit" }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(ItemImportStatus.Confirmed);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items.ShouldAllBe(i => i.Created);
        result.Value.Items.ShouldAllBe(i => i.CategoryCreated);

        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(2);
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);

        var reloaded = await context.ItemImports.SingleAsync(i => i.Id == import.Id, CancellationToken.None);
        reloaded.Status.ShouldBe(ItemImportStatus.Confirmed);
    }

    [Test]
    public async Task Handle_ReusesExistingItemBySkuCaseInsensitively()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Dairy" };
        context.Categories.Add(category);
        var existingItem = new Item
        {
            Sku = "SKU-1",
            Name = "Milk 1L",
            Unit = "L",
            Category = category,
            CategoryId = category.Id
        };
        context.Items.Add(existingItem);
        await context.SaveChangesAsync(CancellationToken.None);

        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);

        var command = new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items = [new ConfirmItemImportItem { Sku = "sku-1", Name = "Milk", CategoryName = "Dairy", Unit = "L" }]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Created.ShouldBeFalse();
        result.Value.Items[0].Id.ShouldBe(existingItem.Id);

        // No duplicate item was created for the matched SKU.
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Handle_AutoCreatesMissingCategoryButReusesExistingCategoryCaseInsensitively()
    {
        await using var context = CreateContext();
        var existingCategory = new Category { Name = "Dairy" };
        context.Categories.Add(existingCategory);
        await context.SaveChangesAsync(CancellationToken.None);

        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);

        var command = new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items =
            [
                new ConfirmItemImportItem { Sku = "SKU-1", Name = "Milk", CategoryName = "dairy", Unit = "L" },
                new ConfirmItemImportItem { Sku = "SKU-2", Name = "Bread", CategoryName = "Bakery", Unit = "unit" }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Single(i => i.Sku == "SKU-1").CategoryCreated.ShouldBeFalse();
        result.Value.Items.Single(i => i.Sku == "SKU-1").CategoryId.ShouldBe(existingCategory.Id);
        result.Value.Items.Single(i => i.Sku == "SKU-2").CategoryCreated.ShouldBeTrue();

        // one existing category reused, one new category created
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task Handle_DeduplicatesItemsWithSameSkuWithinRequest()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);

        var command = new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items =
            [
                new ConfirmItemImportItem { Sku = "  SKU-1  ", Name = "Milk", CategoryName = "Dairy", Unit = "L" },
                new ConfirmItemImportItem
                {
                    Sku = "sku-1", Name = "Milk (duplicate)", CategoryName = "Dairy", Unit = "L"
                }
            ]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(1);
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Handle_RepeatedConfirmation_IsIdempotentAndReturnsStoredResult()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);

        var command = new ConfirmItemImportCommand
        {
            ImportId = import.Id,
            Items = [new ConfirmItemImportItem { Sku = "SKU-1", Name = "Milk", CategoryName = "Dairy", Unit = "L" }]
        };

        var first = await handler.Handle(command, CancellationToken.None);

        // Second confirmation with a different list must not create anything else - the import is
        // already Confirmed and returns the result recorded at confirm time.
        var second =
            await handler.Handle(
                new ConfirmItemImportCommand
                {
                    ImportId = import.Id,
                    Items =
                    [
                        new ConfirmItemImportItem
                        {
                            Sku = "SKU-2", Name = "Bread", CategoryName = "Bakery", Unit = "unit"
                        }
                    ]
                }, CancellationToken.None);

        second.IsSuccess.ShouldBeTrue();
        second.Value.Items.Select(i => i.Id).ShouldBe(first.Value.Items.Select(i => i.Id));
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var handler = new ConfirmItemImportCommandHandler(context);

        var result =
            await handler.Handle(
                new ConfirmItemImportCommand
                {
                    ImportId = Guid.NewGuid(),
                    Items = [new ConfirmItemImportItem { Name = "Milk", CategoryName = "Dairy" }]
                }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenImportNotPendingReview_ReturnsFailure()
    {
        await using var context = CreateContext();
        var import = ItemImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/items.pdf");
        import.Status = ItemImportStatus.Processing;
        context.ItemImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ConfirmItemImportCommandHandler(context);

        var result =
            await handler.Handle(
                new ConfirmItemImportCommand
                {
                    ImportId = import.Id,
                    Items = [new ConfirmItemImportItem { Name = "Milk", CategoryName = "Dairy" }]
                }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(0);
    }

    [Test]
    public async Task Handle_WhenReviewedRowHasNoCategory_RejectsConfirmation()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmItemImportCommandHandler(context);

        var result =
            await handler.Handle(
                new ConfirmItemImportCommand
                {
                    ImportId = import.Id, Items = [new ConfirmItemImportItem { Name = "Milk", CategoryName = " " }]
                }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.Items.CountAsync(CancellationToken.None)).ShouldBe(0);
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(0);
    }
}
