using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        // Restrict on all FKs: Item/Location/SchoolClass are each referenced from multiple
        // entities (StockBatch, StockTransaction, ClassBalance), so cascading deletes here
        // would create multiple cascade paths, which SQL Server rejects.
        builder.HasOne(b => b.Item)
            .WithMany(i => i.Batches)
            .HasForeignKey(b => b.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Location)
            .WithMany(l => l.Batches)
            .HasForeignKey(b => b.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.ReceivedClass)
            .WithMany(c => c.BatchesReceived)
            .HasForeignKey(b => b.ReceivedClassId)
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
        
        builder.Property(b => b.UnitPrice).HasColumnType("decimal(10,2)");

        // Optimistic-concurrency token. SQL Server maintains this rowversion automatically on every
        // UPDATE; EF adds it to the WHERE clause of updates so a stale write affects zero rows and
        // throws DbUpdateConcurrencyException instead of overwriting a concurrent Quantity change.
        builder.Property(b => b.Version).IsRowVersion();
    }
}
