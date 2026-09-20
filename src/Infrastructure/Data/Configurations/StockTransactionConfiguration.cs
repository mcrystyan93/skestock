using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class StockTransactionConfiguration : IEntityTypeConfiguration<StockTransaction>
{
    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.Property(t => t.Reason).HasMaxLength(250);

        // The stock report filters by class/item/location and computes the latest CreatedAt for
        // each item/location pair. Keeping CreatedAt in the key supports the grouped MAX without
        // lookups and also supports a future TOP(1) latest-transaction query shape.
        builder.HasIndex(t => new { t.ClassId, t.ItemId, t.LocationId, t.CreatedAt })
            .HasDatabaseName("IX_StockTransactions_ClassId_ItemId_LocationId_CreatedAt")
            .IsDescending(false, false, false, true);

        // Restrict on all FKs to avoid multiple cascade paths from Item/Location/SchoolClass
        // being referenced by several dependent entities (see StockBatchConfiguration).
        builder.HasOne(t => t.Item)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Location)
            .WithMany(l => l.Transactions)
            .HasForeignKey(t => t.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Class)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Batch)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // UserId links to UserProfile.IdentityId (the AspNetUsers id), not UserProfile.Id -
        // matching CreatedBy/LastModifiedBy below and IUser.Id (the identity id) as set by the
        // caller/handler and the AuditableEntityInterceptor.
        builder.HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .HasPrincipalKey(u => u.IdentityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.CreatedBy)
            .WithMany()
            .HasForeignKey(u => u.CreatedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.LastModifiedBy)
            .WithMany()
            .HasForeignKey(u => u.LastModifiedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
