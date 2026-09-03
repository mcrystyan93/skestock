using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Commands.EditItem;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.Items.Commands.EditItem;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler/validator tests. See
/// CreateItemCommandHandlerTests' ItemTestDbContext for rationale - this mirrors it for the
/// EditItem namespace.
/// </summary>
public class ItemTestDbContext(DbContextOptions<ItemTestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Category> Categories => Set<Category>();
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
        });

        builder.Entity<Item>(b =>
        {
            b.HasOne(i => i.CreatedBy).WithMany().HasForeignKey(i => i.CreatedById);
            b.HasOne(i => i.LastModifiedBy).WithMany().HasForeignKey(i => i.LastModifiedById);
            b.HasOne(i => i.Category).WithMany(c => c.Items).HasForeignKey(i => i.CategoryId);
            b.Ignore(i => i.Batches);
            b.Ignore(i => i.Transactions);
            b.Ignore(i => i.ClassBalances);
        });

        builder.Ignore<Location>();
        builder.Ignore<SchoolClass>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class EditItemCommandHandlerTests
{
    private static async Task<(ItemTestDbContext Context, Category Category)> CreateContextAsync()
    {
        var options = new DbContextOptionsBuilder<ItemTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ItemTestDbContext(options);
        var category = new Category { Name = "Stationery" };
        context.Categories.Add(category);
        await context.SaveChangesAsync(CancellationToken.None);

        return (context, category);
    }

    [Test]
    public async Task Handle_WithExistingItem_UpdatesFieldsAndReturnsDto()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var item = new Item { Name = "Old Name", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new EditItemCommandHandler(context);
        var result = await handler.Handle(new EditItemCommand
        {
            Id = item.Id,
            Name = "New Name",
            Unit = "box",
            MinThreshold = 10,
            IsPerishable = true,
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("New Name");
        result.Value.Unit.ShouldBe("box");
        result.Value.MinThreshold.ShouldBe(10);
        result.Value.IsPerishable.ShouldBeTrue();

        var persisted = await context.Items.SingleAsync(i => i.Id == item.Id, CancellationToken.None);
        persisted.Name.ShouldBe("New Name");
    }

    [Test]
    public async Task Handle_DoesNotChangeIsActive()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var item = new Item { Name = "Old Name", Unit = "unit", CategoryId = category.Id, IsActive = false };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new EditItemCommandHandler(context);
        var result = await handler.Handle(new EditItemCommand
        {
            Id = item.Id,
            Name = "New Name",
            Unit = "unit",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_WithNewCategoryId_UpdatesRelationshipAndReturnsNewCategoryName()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var otherCategory = new Category { Name = "Electronics" };
        context.Categories.Add(otherCategory);
        await context.SaveChangesAsync(CancellationToken.None);

        var item = new Item { Name = "Widget", Unit = "unit", CategoryId = category.Id };
        context.Items.Add(item);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new EditItemCommandHandler(context);
        var result = await handler.Handle(new EditItemCommand
        {
            Id = item.Id,
            Name = "Widget",
            Unit = "unit",
            CategoryId = otherCategory.Id
        }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.CategoryId.ShouldBe(otherCategory.Id);
        result.Value.CategoryName.ShouldBe("Electronics");
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        var (context, category) = await CreateContextAsync();
        await using var _ = context;
        var handler = new EditItemCommandHandler(context);

        var result = await handler.Handle(new EditItemCommand
        {
            Id = Guid.NewGuid(),
            Name = "Anything",
            Unit = "unit",
            CategoryId = category.Id
        }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is skestock.Application.Common.Errors.ItemErrors.ItemNotFound);
    }
}
