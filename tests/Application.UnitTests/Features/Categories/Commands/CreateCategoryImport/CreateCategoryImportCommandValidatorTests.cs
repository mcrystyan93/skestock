using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Categories.Commands.CreateCategoryImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Commands.CreateCategoryImport;

public class CreateCategoryImportCommandValidatorTests
{
    private static async Task<(CategoryImportTestDbContext Context, FileMetadata File)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<CategoryImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new CategoryImportTestDbContext(options);

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
        var validator = new CreateCategoryImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryImportCommand { FileMetadataId = file.Id });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenFileMetadataIdIsEmpty()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateCategoryImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryImportCommand { FileMetadataId = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "FileMetadataId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenFileMetadataDoesNotExist()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateCategoryImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryImportCommand { FileMetadataId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "FileMetadataId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenFileMetadataIsNotConfirmed()
    {
        var (context, file) = await CreateContextAsync();
        await using var _ = context;
        file.Status = FileStatus.Pending;
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new CreateCategoryImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateCategoryImportCommand { FileMetadataId = file.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "FileMetadataId");
    }
}
