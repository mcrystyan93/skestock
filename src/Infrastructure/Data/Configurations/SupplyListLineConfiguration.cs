using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class SupplyListLineConfiguration : IEntityTypeConfiguration<SupplyListLine>
{
    public void Configure(EntityTypeBuilder<SupplyListLine> builder)
    {
        builder.Property(l => l.Unit)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.Property(l => l.Quantity)
            .HasColumnType("decimal(18,3)");

        builder.HasIndex(l => new { l.SupplyListId, l.ItemId })
            .IsUnique();

        builder.HasOne(l => l.SupplyList)
            .WithMany(o => o.Lines)
            .HasForeignKey(l => l.SupplyListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Item)
            .WithMany()
            .HasForeignKey(l => l.ItemId)
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
