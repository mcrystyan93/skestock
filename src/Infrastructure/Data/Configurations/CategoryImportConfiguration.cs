using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class CategoryImportConfiguration : IEntityTypeConfiguration<CategoryImport>
{
    public void Configure(EntityTypeBuilder<CategoryImport> builder)
    {
        builder.Property(i => i.BlobPath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(i => i.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasOne(i => i.FileMetadata)
            .WithMany()
            .HasForeignKey(i => i.FileMetadataId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.UploadedByUser)
            .WithMany()
            .HasForeignKey(i => i.UploadedByUserId)
            .HasPrincipalKey(u => u.IdentityId)
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

        // Composite indexes matching the (column, Id) tie-breaker pairs used by keyset pagination in
        // GetAllCategoryImportsQuery/CategoryImportSortConfiguration.
        builder.HasIndex(i => new { i.CreatedDate, i.Id }).HasDatabaseName("IX_CategoryImports_CreatedDate_Id");
        builder.HasIndex(i => new { i.UploadedAt, i.Id }).HasDatabaseName("IX_CategoryImports_UploadedAt_Id");
    }
}
