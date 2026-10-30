using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities.Items;
using skestock.Domain.Entities.Locations;

namespace skestock.Infrastructure.Data.Configurations;

public class ItemLocationThresholdConfiguration : IEntityTypeConfiguration<ItemLocationThreshold>
{
    public void Configure(EntityTypeBuilder<ItemLocationThreshold> builder)
    {
        builder.HasIndex(x => new { x.ItemId, x.LocationId })
            .IsUnique();

        builder.HasOne<Item>()
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(x => x.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
