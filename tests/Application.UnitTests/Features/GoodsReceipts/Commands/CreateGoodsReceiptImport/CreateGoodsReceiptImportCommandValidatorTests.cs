using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;
using skestock.Domain.Entities;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;

public class CreateGoodsReceiptImportCommandValidatorTests
{
    private static async Task<(GoodsReceiptImportTestDbContext Context, SchoolClass Class, FileMetadata File)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<GoodsReceiptImportTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new GoodsReceiptImportTestDbContext(options);

        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 20)
        };
        context.SchoolClasses.Add(schoolClass);

        var file = new FileMetadata
        {
            FileId = Guid.NewGuid(),
            OriginalName = "receipt.pdf",
            BlobContainer = "app-files",
            BlobPath = "imports/2026/receipt.pdf",
            ContentType = "application/pdf",
            SizeBytes = 1024,
            Status = FileStatus.Completed
        };
        context.FileMetadata.Add(file);

        await context.SaveChangesAsync(CancellationToken.None);

        return (context, schoolClass, file);
    }

    [Test]
    public async Task ShouldNotHaveErrorsForValidCommand()
    {
        var (context, schoolClass, file) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = file.Id
        });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenClassIdIsEmpty()
    {
        var (context, _, file) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = Guid.Empty,
            FileMetadataId = file.Id
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "ClassId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenClassDoesNotExist()
    {
        var (context, _, file) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = Guid.NewGuid(),
            FileMetadataId = file.Id
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "ClassId");
    }

    [Test]
    public async Task ShouldHaveRequiredErrorWhenFileMetadataIdIsEmpty()
    {
        var (context, schoolClass, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = Guid.Empty
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.Required && e.PropertyName == "FileMetadataId");
    }

    [Test]
    public async Task ShouldHaveInvalidReferenceErrorWhenFileMetadataDoesNotExist()
    {
        var (context, schoolClass, _) = await CreateContextAsync();
        await using var _ = context;
        var validator = new CreateGoodsReceiptImportCommandValidator(context);

        var result = await validator.ValidateAsync(new CreateGoodsReceiptImportCommand
        {
            ClassId = schoolClass.Id,
            FileMetadataId = Guid.NewGuid()
        });

        result.Errors.ShouldContain(e => e.ErrorCode == ValidationErrorCodes.InvalidReference && e.PropertyName == "FileMetadataId");
    }
}
