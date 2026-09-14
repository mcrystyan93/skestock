using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Models.Options;
using skestock.Application.Features.Categories.Commands.CreateCategoryImportBatch;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Commands.CreateCategoryImportBatch;

public class CreateCategoryImportBatchCommandValidatorTests
{
    private static async Task<(CategoryImportBatchTestDbContext Context, FileMetadata File)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<CategoryImportBatchTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CategoryImportBatchTestDbContext(options);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "categories.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/categories.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, file);
    }

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var (context, file) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateCategoryImportBatchCommandValidator(
            context, Options.Create(new ImportBatchOptions()), new FakeUser(file.CreatedById));

        var result = await validator.ValidateAsync(new CreateCategoryImportBatchCommand { FileMetadataIds = [file.Id] });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenFileMetadataIdIsEmpty()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateCategoryImportBatchCommandValidator(
            context, Options.Create(new ImportBatchOptions()), new FakeUser(null));

        var result = await validator.ValidateAsync(new CreateCategoryImportBatchCommand { FileMetadataIds = [] });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required &&
                                         e.PropertyName == "FileMetadataIds");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenFileMetadataDoesNotExist()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateCategoryImportBatchCommandValidator(
            context, Options.Create(new ImportBatchOptions()), new FakeUser(null));

        var result = await validator.ValidateAsync(new CreateCategoryImportBatchCommand
        {
            FileMetadataIds = [Guid.NewGuid()]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference &&
                                         e.PropertyName == "FileMetadataIds");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenFileMetadataIsNotConfirmed()
    {
        var (context, file) = await CreateContextAsync();
        await using var _ = context;
        file.Status = FileStatus.Pending;
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new CreateCategoryImportBatchCommandValidator(
            context, Options.Create(new ImportBatchOptions()), new FakeUser(file.CreatedById));

        var result = await validator.ValidateAsync(new CreateCategoryImportBatchCommand
        {
            FileMetadataIds = [file.Id]
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference &&
                                         e.PropertyName == "FileMetadataIds");
    }
}
