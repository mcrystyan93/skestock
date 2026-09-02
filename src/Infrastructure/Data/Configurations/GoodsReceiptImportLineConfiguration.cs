using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class GoodsReceiptImportLineConfiguration : IEntityTypeConfiguration<GoodsReceiptImportLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptImportLine> builder)
    {
        builder.Property(l => l.RawItemText)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.UnitPrice)
            .HasColumnType("decimal(10,2)");

        builder.HasOne(l => l.Import)
            .WithMany(i => i.Lines)
            .HasForeignKey(l => l.ImportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.MatchedItem)
            .WithMany()
            .HasForeignKey(l => l.MatchedItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Location)
            .WithMany()
            .HasForeignKey(l => l.LocationId)
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
