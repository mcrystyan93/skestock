using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Categories.Queries.GetCategoryImportById;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetCategoryImportById;

public class GetCategoryImportByIdHandlerTests
{
    private static CategoryImportReviewTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CategoryImportReviewTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CategoryImportReviewTestDbContext(options);
    }

    [Test]
    public async Task Handle_ReturnsSuggestionsFlaggingExistingCategoriesCaseInsensitively()
    {
        await using var context = CreateContext();

        context.Categories.Add(new Category { Name = "Dairy" });

        var import = CategoryImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/categories.pdf");
        var extraction = new CategoryExtractionResult
        {
            Categories = [new ExtractedCategory { Name = "dairy" }, new ExtractedCategory { Name = "Bakery" }]
        };
        import.ApplyExtractionResult(JsonSerializer.Serialize(extraction));
        context.CategoryImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCategoryImportByIdHandler(context);

        var result = await handler.Handle(new GetCategoryImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(CategoryImportStatus.PendingReview);
        result.Value.Suggestions.Count.ShouldBe(2);
        result.Value.Suggestions.Single(s => s.Name == "dairy").AlreadyExists.ShouldBeTrue();
        result.Value.Suggestions.Single(s => s.Name == "Bakery").AlreadyExists.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WhenImportDoesNotExist_ReturnsNotFoundFailure()
    {
        await using var context = CreateContext();
        var handler = new GetCategoryImportByIdHandler(context);

        var result = await handler.Handle(new GetCategoryImportByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenNoExtractionYet_ReturnsEmptySuggestions()
    {
        await using var context = CreateContext();

        var import = CategoryImport.Create(Guid.NewGuid(), Guid.NewGuid(), "blob/categories.pdf");
        context.CategoryImports.Add(import);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCategoryImportByIdHandler(context);

        var result = await handler.Handle(new GetCategoryImportByIdQuery { Id = import.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Status.ShouldBe(CategoryImportStatus.Processing);
        result.Value.Suggestions.ShouldBeEmpty();
    }
}
