using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class DailyItemConsumptionConfiguration : IEntityTypeConfiguration<DailyItemConsumption>
{
    public void Configure(EntityTypeBuilder<DailyItemConsumption> builder)
    {
        // Restrict on all FKs to avoid multiple cascade paths (see StockBatchConfiguration).
        builder.HasOne(c => c.Item)
            .WithMany()
            .HasForeignKey(c => c.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Class)
            .WithMany()
            .HasForeignKey(c => c.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Location)
            .WithMany()
            .HasForeignKey(c => c.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.TotalValue).HasColumnType("decimal(18,2)");

        builder.HasIndex(c => new { c.Date, c.ItemId, c.ClassId, c.LocationId })
            .IsUnique()
            .HasDatabaseName("IX_DailyItemConsumptions_Date_ItemId_ClassId_LocationId");

        // Charts read one item's (or one class's) consumption over a date range.
        builder.HasIndex(c => new { c.ItemId, c.Date })
            .HasDatabaseName("IX_DailyItemConsumptions_ItemId_Date")
            .IncludeProperties(c => new { c.Quantity, c.TotalValue });

        builder.HasIndex(c => new { c.ClassId, c.Date })
            .HasDatabaseName("IX_DailyItemConsumptions_ClassId_Date")
            .IncludeProperties(c => new { c.Quantity, c.TotalValue });
    }
}
