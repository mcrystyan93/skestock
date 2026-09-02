using FluentResults;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;
using skestock.Domain.Entities;
using skestock.Domain.Queues;
using skestock.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace skestock.Application.UnitTests.Features.SchoolClasses.Queries.GetSchoolClassById;

/// <summary>
/// Minimal <see cref="IApplicationDbContext"/> implementation for handler tests. See
/// CreateSchoolClassCommandHandlerTests' SchoolClassTestDbContext for rationale - this mirrors it
/// for the GetSchoolClassById namespace.
/// </summary>
public class SchoolClassTestDbContext(DbContextOptions<SchoolClassTestDbContext> options)
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

        builder.Entity<SchoolClass>(b =>
        {
            b.HasOne(c => c.CreatedBy).WithMany().HasForeignKey(c => c.CreatedById);
            b.HasOne(c => c.LastModifiedBy).WithMany().HasForeignKey(c => c.LastModifiedById);
            b.Ignore(c => c.BatchesReceived);
            b.Ignore(c => c.Transactions);
            b.Ignore(c => c.Balances);
        });

        builder.Ignore<Category>();
        builder.Ignore<ClassBalance>();
        builder.Ignore<Item>();
        builder.Ignore<Location>();
        builder.Ignore<StockBatch>();
        builder.Ignore<StockTransaction>();
        builder.Ignore<GoodsReceipt>();
    }
}

public class GetSchoolClassByIdHandlerTests
{
    private static SchoolClassTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SchoolClassTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SchoolClassTestDbContext(options);
    }

    [Test]
    public async Task Handle_WithExistingId_ReturnsMatchingSchoolClassDto()
    {
        await using var context = CreateContext();
        var schoolClass = new SchoolClass
        {
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 1),
            Status = ClassStatus.Active
        };
        context.SchoolClasses.Add(schoolClass);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetSchoolClassByIdHandler(context);
        var result = await handler.Handle(new GetSchoolClassByIdQuery { Id = schoolClass.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(schoolClass.Id);
        result.Value.Name.ShouldBe("Fall 2026");
        result.Value.Status.ShouldBe(ClassStatus.Active);
        result.Value.StartDate.ShouldBe(new DateOnly(2026, 9, 1));
        result.Value.EndDate.ShouldBe(new DateOnly(2026, 12, 1));
    }

    [Test]
    public async Task Handle_WithNonExistentId_ReturnsFailedResult()
    {
        await using var context = CreateContext();
        var handler = new GetSchoolClassByIdHandler(context);

        var result = await handler.Handle(new GetSchoolClassByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailed.ShouldBeTrue();
    }
}
