using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Documents.Models;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Queries.GetCategoryImportBatchById;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetCategoryImportBatchById;

public class GetCategoryImportBatchByIdHandlerTests
{
    private sealed class TestUser(Guid? id) : IUser
    {
        public Guid? Id { get; } = id;
        public List<string>? Roles { get; } = [];
    }

    private static CategoryImportBatchReviewTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CategoryImportBatchReviewTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CategoryImportBatchReviewTestDbContext(options);
    }

    [Test]
    public async Task Handle_ReturnsSuggestionsFlaggingExistingCategoriesCaseInsensitively()
    {
        await using var context = CreateContext();

        context.Categories.Add(new Category { Name = "Dairy" });

        var import = CategoryImportBatch.Create(Guid.NewGuid(), null, [Guid.NewGuid()]);
        var extraction = new CategoryExtractionResult
        {
            Categories = [new ExtractedCategory { Name = "dairy" }, new ExtractedCategory { Name = "Bakery" }]
        };
        import.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
        context.CategoryImportBatches.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCategoryImportBatchByIdHandler(context, new TestUser(import.UploadedByUserId));

        var result = await handler.Handle(new GetCategoryImportBatchByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.PendingReview);
        result.Value.Suggestions.Count.ShouldBe(2);
        var matched = result.Value.Suggestions.Single(s => s.Name == "dairy");
        matched.AlreadyExists.ShouldBeTrue();
        matched.MatchedCategory.ShouldNotBeNull();
        matched.MatchedCategory!.Name.ShouldBe("Dairy");
        matched.MatchedCategory.Id.ShouldBe(context.Categories.Single().Id);
        result.Value.Suggestions.Single(s => s.Name == "Bakery").AlreadyExists.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var handler = new GetCategoryImportBatchByIdHandler(context, new TestUser(Guid.NewGuid()));

        var result = await handler.Handle(new GetCategoryImportBatchByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenNoExtractionYet_ReturnsEmptySuggestions()
    {
        await using var context = CreateContext();

        var import = CategoryImportBatch.Create(Guid.NewGuid(), null, [Guid.NewGuid()]);
        context.CategoryImportBatches.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCategoryImportBatchByIdHandler(context, new TestUser(import.UploadedByUserId));

        var result = await handler.Handle(new GetCategoryImportBatchByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(CategoryImportBatchStatus.Processing);
        result.Value.Suggestions.ShouldBeEmpty();
    }
}
