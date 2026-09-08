using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Commands.UpdateCategory;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Commands.UpdateCategory;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. Only
/// maps <see cref="Category"/> (and the <see cref="UserProfile"/> it references for
/// CreatedBy/LastModifiedBy); all other DbSets required by the interface are left
/// unmapped/ignored since <see cref="UpdateCategoryCommandHandler"/> never touches them. Mirrors
/// CreateCategory's CategoryTestDbContext and avoids EF's "ambiguous one-to-one relationship"
/// error the full production model would trigger for UserProfile.
/// </summary>
public class CategoryTestDbContext(DbContextOptions<CategoryTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<CategoryImport> CategoryImports => Set<CategoryImport>();
    public DbSet<ClassBalance> ClassBalances => Set<ClassBalance>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileMetadata> FileMetadata => Set<FileMetadata>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    public DbSet<GoodsReceiptImport> GoodsReceiptImports => Set<GoodsReceiptImport>();
    public DbSet<GoodsReceiptImportLine> GoodsReceiptImportLines => Set<GoodsReceiptImportLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Ignore<CategoryImport>();

        builder.Ignore<GoodsReceiptImport>();
        builder.Ignore<GoodsReceiptImportLine>();

        builder.Ignore<FileMetadata>();

        builder.Entity<UserProfile>(b =>
        {
            b.Ignore(u => u.CreatedBy);
            b.Ignore(u => u.LastModifiedBy);
        });

        builder.Entity<Category>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.OwnsOne(c => c.Icon);
            b.Ignore(c => c.Items);
        });

        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class UpdateCategoryCommandHandlerTests
{
    private static CategoryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CategoryTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithExistingCategory_UpdatesNameAndReturnsDto()
    {
        await using var context = CreateContext();
        var category = new Category
        {
            Name = "Old Name",
            Icon = new CategoryIcon("Old Icon", "old-icon", "/assets/icons/old-icon.svg")
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateCategoryCommandHandler(context);
        var icon = new CategoryIconDto
        {
            Name = "Square Q",
            FileName = "square-q",
            Path = "/assets/icons/square-q.svg"
        };
        var result = await handler.Handle(
            new UpdateCategoryCommand { Id = category.Id, Name = "New Name", Icon = icon },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(category.Id);
        result.Value.Name.ShouldBe("New Name");
        result.Value.Icon.ShouldBe(icon);

        var persisted = await context.Categories.SingleAsync(c => c.Id == category.Id, CancellationToken.None);
        persisted.Name.ShouldBe("New Name");
        persisted.Icon.ShouldNotBeNull();
        persisted.Icon.Name.ShouldBe(icon.Name);
        persisted.Icon.FileName.ShouldBe(icon.FileName);
        persisted.Icon.Path.ShouldBe(icon.Path);
    }

    [Test]
    public async Task Handle_WithNullIcon_ClearsExistingIcon()
    {
        await using var context = CreateContext();
        var category = new Category
        {
            Name = "Office Supplies",
            Icon = new CategoryIcon("Square Q", "square-q", "/assets/icons/square-q.svg")
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateCategoryCommandHandler(context);
        var result = await handler.Handle(
            new UpdateCategoryCommand { Id = category.Id, Name = category.Name, Icon = null },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Icon.ShouldBeNull();
        (await context.Categories.SingleAsync(c => c.Id == category.Id, CancellationToken.None))
            .Icon.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WithNameContainingWhitespace_TrimsBeforePersisting()
    {
        await using var context = CreateContext();
        var category = new Category { Name = "Old Name" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateCategoryCommandHandler(context);
        var result = await handler.Handle(
            new UpdateCategoryCommand { Id = category.Id, Name = "  Trimmed  " },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Trimmed");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        await using var context = CreateContext();
        var handler = new UpdateCategoryCommandHandler(context);

        var result = await handler.Handle(
            new UpdateCategoryCommand { Id = Guid.NewGuid(), Name = "Anything" },
            CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is CategoryErrors.CategoryNotFound);
    }
}
