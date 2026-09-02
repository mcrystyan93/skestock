using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class GoodsReceiptImportConfiguration : IEntityTypeConfiguration<GoodsReceiptImport>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptImport> builder)
    {
        builder.Property(i => i.BlobPath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(i => i.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasOne(i => i.Class)
            .WithMany()
            .HasForeignKey(i => i.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(i => i.FileMetadata)
            .WithMany()
            .HasForeignKey(i => i.FileMetadataId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.UploadedByUser)
            .WithMany()
            .HasForeignKey(i => i.UploadedByUserId)
            .HasPrincipalKey(u => u.IdentityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ResultingGoodsReceipt)
            .WithMany()
            .HasForeignKey(i => i.ResultingGoodsReceiptId)
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
