using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.ConfirmCategoryImportBatch;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Categories.Commands.ConfirmCategoryImportBatch;

public class ConfirmCategoryImportBatchCommandHandlerTests
{
    private sealed class TestUser(Guid? id) : IUser
    {
        public Guid? Id { get; } = id;
        public List<string>? Roles { get; } = [];
    }
    private static ConfirmCategoryImportBatchTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConfirmCategoryImportBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConfirmCategoryImportBatchTestDbContext(options);
    }

    private static async Task<CategoryImportBatch> AddPendingImportAsync(ConfirmCategoryImportBatchTestDbContext context)
    {
        var import = CategoryImportBatch.Create(Guid.NewGuid(), null, [Guid.NewGuid()]);
        import.Status = CategoryImportBatchStatus.PendingReview;
        context.CategoryImportBatches.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);
        return import;
    }

    [Test]
    public async Task Handle_WithNewNames_CreatesReviewedCategoriesAndMarksConfirmed()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmCategoryImportBatchCommandHandler(context, new TestUser(import.UploadedByUserId));

        var command = new ConfirmCategoryImportBatchCommand
        {
            BatchId = import.Id,
            CategoryNames = ["Dairy", "Bakery"]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.BatchId.ShouldBe(import.Id);
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.Confirmed);

        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);

        var reloaded = await context.CategoryImportBatches.SingleAsync(i => i.Id == import.Id, CancellationToken.None);
        reloaded.Status.ShouldBe(CategoryImportBatchStatus.Confirmed);
        using var storedResult = System.Text.Json.JsonDocument.Parse(reloaded.ConfirmationResultJson!);
        storedResult.RootElement.GetProperty("Categories").GetArrayLength().ShouldBe(2);
    }

    [Test]
    public async Task Handle_NormalizesTrimAndDeduplicatesCaseInsensitively()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmCategoryImportBatchCommandHandler(context, new TestUser(import.UploadedByUserId));

        var command = new ConfirmCategoryImportBatchCommand
        {
            BatchId = import.Id,
            CategoryNames = ["  Dairy  ", "dairy", "DAIRY"]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.BatchId.ShouldBe(import.Id);
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.Confirmed);

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
        var handler = new ConfirmCategoryImportBatchCommandHandler(context, new TestUser(import.UploadedByUserId));

        var command = new ConfirmCategoryImportBatchCommand
        {
            BatchId = import.Id,
            CategoryNames = ["pantry", "Cleaning"]
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.BatchId.ShouldBe(import.Id);
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.Confirmed);

        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);
        (await context.Categories.SingleAsync(c => c.Name == existing.Name, CancellationToken.None))
            .Id.ShouldBe(existing.Id);
    }

    [Test]
    public async Task Handle_RepeatedConfirmation_IsIdempotentAndReturnsStoredResult()
    {
        await using var context = CreateContext();
        var import = await AddPendingImportAsync(context);
        var handler = new ConfirmCategoryImportBatchCommandHandler(context, new TestUser(import.UploadedByUserId));

        var command = new ConfirmCategoryImportBatchCommand
        {
            BatchId = import.Id,
            CategoryNames = ["Dairy", "Bakery"]
        };

        var first = await handler.Handle(command, CancellationToken.None);

        // Second confirmation with a different list must not create anything else - the import is
        // already Confirmed and returns the result recorded at confirm time.
        var second = await handler.Handle(new ConfirmCategoryImportBatchCommand
        {
            BatchId = import.Id,
            CategoryNames = ["Frozen"]
        }, CancellationToken.None);

        second.IsSuccess.ShouldBeTrue();
        second.Value.BatchId.ShouldBe(first.Value.BatchId);
        second.Value.Status.ShouldBe(first.Value.Status);
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(2);
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var handler = new ConfirmCategoryImportBatchCommandHandler(context, new TestUser(null));

        var result = await handler.Handle(new ConfirmCategoryImportBatchCommand
        {
            BatchId = Guid.NewGuid(),
            CategoryNames = ["Dairy"]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenImportNotPendingReview_ReturnsFailure()
    {
        await using var context = CreateContext();
        var import = CategoryImportBatch.Create(Guid.NewGuid(), null, [Guid.NewGuid()]);
        import.Status = CategoryImportBatchStatus.Processing;
        context.CategoryImportBatches.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ConfirmCategoryImportBatchCommandHandler(context, new TestUser(import.UploadedByUserId));

        var result = await handler.Handle(new ConfirmCategoryImportBatchCommand
        {
            BatchId = import.Id,
            CategoryNames = ["Dairy"]
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        (await context.Categories.CountAsync(CancellationToken.None)).ShouldBe(0);
    }
}
