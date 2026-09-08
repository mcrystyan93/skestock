using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Categories.Commands.ConfirmCategoryImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Categories.Commands.ConfirmCategoryImport;

public class ConfirmCategoryImportCommandHandlerTests
{
    private static ConfirmCategoryImportTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConfirmCategoryImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConfirmCategoryImportTestDbContext(options);
    }

    private static async Task<CategoryImport> AddPendingImportAsync(ConfirmCategoryImportTestDbContext context)
    {
        var import = CategoryImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/categories.pdf");
        import.Status = CategoryImportStatus.PendingReview;
        context.CategoryImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);
        return import;
    }

    [Test]
    public async Task Handle_WithNewNames_CreatesReviewedCategoriesAndMarksConfirmed()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmCategoryImportCommandHandler(context);

        var command = new ConfirmCategoryImportCommand
        {
            ImportId = import.Id,
            CategoryNames = ["Dairy", "Bakery"]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(CategoryImportStatus.Confirmed);
        result.Value.Categories.Count.ShouldBe(2);
        result.Value.Categories.ShouldAllBe(c => c.Created);

        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);

        var reloaded = await context.CategoryImports.SingleAsync(i => i.Id == import.Id, CancellationToken.None);
        reloaded.Status.ShouldBe(CategoryImportStatus.Confirmed);
    }

    [Test]
    public async Task Handle_NormalizesTrimAndDeduplicatesCaseInsensitively()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmCategoryImportCommandHandler(context);

        var command = new ConfirmCategoryImportCommand
        {
            ImportId = import.Id,
            CategoryNames = ["  Dairy  ", "dairy", "DAIRY"]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Categories.Count.ShouldBe(1);
        result.Value.Categories[0].Name.ShouldBe("Dairy");

        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(1);
    }

    [Test]
    public async Task Handle_SkipsNamesThatAlreadyExistCaseInsensitively()
    {
        await using var context = CreateContext();
        var existing = new Category { Name = "Pantry" };
        context.Categories.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmCategoryImportCommandHandler(context);

        var command = new ConfirmCategoryImportCommand
        {
            ImportId = import.Id,
            CategoryNames = ["pantry", "Cleaning"]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        // one already existed (not created), one new (created)
        result.Value.Categories.Count(c => c.Created).ShouldBe(1);
        result.Value.Categories.Count(c => !c.Created).ShouldBe(1);

        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);
        result.Value.Categories.Single(c => !c.Created).Id.ShouldBe(existing.Id);
    }

    [Test]
    public async Task Handle_RepeatedConfirmation_IsIdempotentAndReturnsStoredResult()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmCategoryImportCommandHandler(context);

        var command = new ConfirmCategoryImportCommand
        {
            ImportId = import.Id,
            CategoryNames = ["Dairy", "Bakery"]
        };

        var first = await handler.Handle(command, CancellationToken.None);

        // Second confirmation with a different list must not create anything else - the import is
        // already Confirmed and returns the result recorded at confirm time.
        var second = await handler.Handle(new ConfirmCategoryImportCommand
        {
            ImportId = import.Id,
            CategoryNames = ["Frozen"]
        }, CancellationToken.None);

        second.IsSuccess.ShouldBeTrue();
        second.Value.Categories.Select(c => c.Id).ShouldBe(first.Value.Categories.Select(c => c.Id));
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var handler = new ConfirmCategoryImportCommandHandler(context);

        var result = await handler.Handle(new ConfirmCategoryImportCommand
        {
            ImportId = Guid.NewGuid(),
            CategoryNames = ["Dairy"]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenImportNotPendingReview_ReturnsFailure()
    {
        await using var context = CreateContext();
        var import = CategoryImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/categories.pdf");
        import.Status = CategoryImportStatus.Processing;
        context.CategoryImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ConfirmCategoryImportCommandHandler(context);

        var result = await handler.Handle(new ConfirmCategoryImportCommand
        {
            ImportId = import.Id,
            CategoryNames = ["Dairy"]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(0);
    }
}
