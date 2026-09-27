using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;
using skestock.Application.Features.Categories.Queries.GetAllCategoryImportBatches;
using skestock.Application.UnitTests.Features.Categories.Commands.CreateCategoryImportBatch;
using skestock.Domain.Entities;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetAllCategoryImportBatches;

public class GetAllCategoryImportBatchesHandlerTests
{
    [Test]
    public async Task Handle_WithFileNameSearch_ReturnsOnlyMatchingBatch()
    {
        var options = new DbContextOptionsBuilder<CategoryImportBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new CategoryImportBatchTestDbContext(options);
        var user = new UserProfile { IdentityId = Guid.NewGuid() };
        var matchingFile = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "matching.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/matching.pdf",
            ContentType = "application/pdf"
        };
        var otherFile = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "other.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/other.pdf",
            ContentType = "application/pdf"
        };
        var matchingBatch = CategoryImportBatch.Create(user.IdentityId, null, [matchingFile.Id]);
        var otherBatch = CategoryImportBatch.Create(user.IdentityId, null, [otherFile.Id]);
        matchingBatch.UploadedByUser = user;
        otherBatch.UploadedByUser = user;
        matchingBatch.Files.Single().FileMetadata = matchingFile;
        otherBatch.Files.Single().FileMetadata = otherFile;
        context.CategoryImportBatches.AddRange(matchingBatch, otherBatch);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetAllCategoryImportBatchesHandler(context);
        var result = await handler.Handle(
            new GetAllCategoryImportBatchesQuery { SearchTerm = " matching " }, CancellationToken.None);

        result.Value.Data.Single().Id.ShouldBe(matchingBatch.Id);
    }
}
