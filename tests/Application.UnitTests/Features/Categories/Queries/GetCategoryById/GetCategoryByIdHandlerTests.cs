using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Application.Features.Categories.Queries.GetCategoryById;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Categories.Queries.GetCategoryById;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. Only maps
/// <see cref="Category"/> (and the <see cref="UserProfile"/> it references for
/// CreatedBy/LastModifiedBy); all other DbSets required by the interface are left
/// unmapped/ignored since <see cref="GetCategoryByIdHandler"/> never touches them. This mirrors
/// the equivalent test double in CreateCategoryCommandHandlerTests / GetAllCategoriesHandlerTests
/// and avoids EF's "ambiguous one-to-one relationship" error the full production model would
/// trigger for UserProfile.
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
        });

        builder.Entity<Item>(b =>
        {
            b.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            b.Ignore(i => i.Batches);
            b.Ignore(i => i.Transactions);
            b.Ignore(i => i.ClassBalances);
            b.Ignore(i => i.CreatedBy);
            b.Ignore(i => i.LastModifiedBy);
        });

        builder.Ignore<ClassBalance>();
        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class GetCategoryByIdHandlerTests
{
    private static CategoryTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CategoryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CategoryTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithExistingId_ReturnsMatchingCategoryDto()
    {
        await using var context = CreateContext();
        var category = new Category
        {
            Name = "Stationery",
            Icon = new CategoryIcon("Square Q", "square-q", "/assets/icons/square-q.svg")
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);
        context.Items.AddRange(
            new Item { Name = "Notebook", CategoryId = category.Id },
            new Item { Name = "Pen", CategoryId = category.Id });
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetCategoryByIdHandler(context);
        var result = await handler.Handle(new GetCategoryByIdQuery { Id = category.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(category.Id);
        result.Value.Name.ShouldBe("Stationery");
        result.Value.ItemCount.ShouldBe(2);
        result.Value.Icon.ShouldNotBeNull();
        result.Value.Icon.Name.ShouldBe("Square Q");
        result.Value.Icon.FileName.ShouldBe("square-q");
        result.Value.Icon.Path.ShouldBe("/assets/icons/square-q.svg");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        await using var context = CreateContext();
        var handler = new GetCategoryByIdHandler(context);

        var result = await handler.Handle(new GetCategoryByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }
}
