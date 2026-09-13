using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class ItemImportBatchFileConfiguration : IEntityTypeConfiguration<ItemImportBatchFile>
{
    public void Configure(EntityTypeBuilder<ItemImportBatchFile> builder)
    {
        builder.HasOne(f => f.Batch)
            .WithMany(b => b.Files)
            .HasForeignKey(f => f.ItemImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.FileMetadata)
            .WithMany()
            .HasForeignKey(f => f.FileMetadataId)
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

        builder.HasIndex(f => new { f.ItemImportBatchId, f.FileMetadataId })
            .HasDatabaseName("IX_ItemImportBatchFiles_BatchId_FileMetadataId")
            .IsUnique();
    }
}
