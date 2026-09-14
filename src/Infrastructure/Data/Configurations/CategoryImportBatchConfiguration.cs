using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class CategoryImportBatchConfiguration : IEntityTypeConfiguration<CategoryImportBatch>
{
    public void Configure(EntityTypeBuilder<CategoryImportBatch> builder)
    {
        builder.Property(b => b.ErrorMessage).HasMaxLength(2000);

        builder.HasOne(b => b.UploadedByUser)
            .WithMany()
            .HasForeignKey(b => b.UploadedByUserId)
            .HasPrincipalKey(u => u.IdentityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.CreatedBy)
            .WithMany()
            .HasForeignKey(b => b.CreatedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.LastModifiedBy)
            .WithMany()
            .HasForeignKey(b => b.LastModifiedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasIndex(b => new { b.UploadedByUserId, b.ClientRequestId })
            .HasDatabaseName("IX_CategoryImportBatches_UploadedByUserId_ClientRequestId")
            .IsUnique()
            .HasFilter("[ClientRequestId] IS NOT NULL");

        builder.HasIndex(b => new { b.CreatedDate, b.Id })
            .HasDatabaseName("IX_CategoryImportBatches_CreatedDate_Id");
        builder.HasIndex(b => new { b.UploadedAt, b.Id })
            .HasDatabaseName("IX_CategoryImportBatches_UploadedAt_Id");

        builder.OwnsMany(b => b.History, history =>
        {
            history.ToTable("CategoryImportBatchHistory");
            history.WithOwner().HasForeignKey("CategoryImportBatchId");
            history.HasKey(h => h.Id);
            history.Property(h => h.Id).ValueGeneratedNever();
            history.Property(h => h.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            history.Property(h => h.Message).HasMaxLength(2000);
            history.HasIndex(h => h.CreatedAtUtc);
        });
    }
}
