using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class OrderListLineConfiguration : IEntityTypeConfiguration<OrderListLine>
{
    public void Configure(EntityTypeBuilder<OrderListLine> builder)
    {
        builder.Property(l => l.ProductName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.Unit)
            .HasMaxLength(50);

        builder.Property(l => l.Notes)
            .HasMaxLength(1000);

        builder.Property(l => l.Quantity)
            .HasColumnType("decimal(18,3)");

        builder.HasOne(l => l.OrderList)
            .WithMany(o => o.Lines)
            .HasForeignKey(l => l.OrderListId)
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
