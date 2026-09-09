using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.Items.Commands.CreateItemImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.CreateItemImport;

public class CreateItemImportCommandValidatorTests
{
    private static async Task<(ItemImportTestDbContext Context, FileMetadata File)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ItemImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemImportTestDbContext(options);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "items.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/items.pdf",
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
        var validator = new CreateItemImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemImportCommand { FileMetadataId = file.Id });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenFileMetadataIdIsEmpty()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemImportCommand { FileMetadataId = Guid.Empty });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "FileMetadataId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenFileMetadataDoesNotExist()
    {
        var (context, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateItemImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemImportCommand { FileMetadataId = Guid.NewGuid() });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "FileMetadataId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenFileMetadataIsNotConfirmed()
    {
        var (context, file) = await CreateContextAsync();
        await using var _ = context;
        file.Status = FileStatus.Pending;
        await context.SaveChangesAsync(CancellationToken.None);
        var validator = new CreateItemImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateItemImportCommand { FileMetadataId = file.Id });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "FileMetadataId");
    }
}
