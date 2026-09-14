using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class CategoryImportBatchFileConfiguration : IEntityTypeConfiguration<CategoryImportBatchFile>
{
    public void Configure(EntityTypeBuilder<CategoryImportBatchFile> builder)
    {
        builder.HasOne(file => file.Batch)
            .WithMany(batch => batch.Files)
            .HasForeignKey(file => file.CategoryImportBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(file => file.FileMetadata)
            .WithMany()
            .HasForeignKey(file => file.FileMetadataId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(file => file.CreatedBy)
            .WithMany()
            .HasForeignKey(file => file.CreatedById)
            .HasPrincipalKey(user => user.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(file => file.LastModifiedBy)
            .WithMany()
            .HasForeignKey(file => file.LastModifiedById)
            .HasPrincipalKey(user => user.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(file => new { file.CategoryImportBatchId, file.FileMetadataId })
            .HasDatabaseName("IX_CategoryImportBatchFiles_BatchId_FileMetadataId")
            .IsUnique();
    }
}
