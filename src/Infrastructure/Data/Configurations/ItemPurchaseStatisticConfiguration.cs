using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class ItemPurchaseStatisticConfiguration : IEntityTypeConfiguration<ItemPurchaseStatistic>
{
    public void Configure(EntityTypeBuilder<ItemPurchaseStatistic> builder)
    {
        // Restrict on all FKs to avoid multiple cascade paths (see StockBatchConfiguration).
        builder.HasOne(s => s.Item)
            .WithMany()
            .HasForeignKey(s => s.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Class)
            .WithMany()
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.Scope).HasConversion<string>().HasMaxLength(32);
        builder.Property(s => s.TotalValue).HasColumnType("decimal(18,2)");
        builder.Property(s => s.AverageQuantity).HasColumnType("decimal(18,2)");
        builder.Property(s => s.AverageUnitPrice).HasColumnType("decimal(18,2)");

        builder.HasIndex(s => new { s.Scope, s.ClassId, s.ItemId })
            .IsUnique()
            // No IS NOT NULL filter: global scopes (ClassId == null) must be unique per item too.
            .HasFilter(null)
            .HasDatabaseName("IX_ItemPurchaseStatistics_Scope_ClassId_ItemId");

        builder.HasIndex(s => new { s.ItemId, s.Scope })
            .HasDatabaseName("IX_ItemPurchaseStatistics_ItemId_Scope");
    }
}
