using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class ItemImportBatchConfiguration : IEntityTypeConfiguration<ItemImportBatch>
{
    public void Configure(EntityTypeBuilder<ItemImportBatch> builder)
    {
        builder.Property(b => b.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasOne(b => b.UploadedByUser)
            .WithMany()
            .HasForeignKey(b => b.UploadedByUserId)
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

        builder.Property(b => b.ConcurrencyStamp).IsConcurrencyToken();

        builder.HasIndex(b => new { b.UploadedByUserId, b.ClientRequestId })
            .HasDatabaseName("IX_ItemImportBatches_UploadedByUserId_ClientRequestId")
            .IsUnique()
            .HasFilter("[ClientRequestId] IS NOT NULL");

        builder.HasIndex(b => new { b.CreatedDate, b.Id }).HasDatabaseName("IX_ItemImportBatches_CreatedDate_Id");
        builder.HasIndex(b => new { b.UploadedAt, b.Id }).HasDatabaseName("IX_ItemImportBatches_UploadedAt_Id");
        builder.HasIndex(b => new { b.LastModifiedDate, b.Id })
            .HasDatabaseName("IX_ItemImportBatches_LastModifiedDate_Id");

        // Supports listing filtered by Status combined with the default CreatedDate desc, Id desc sort/keyset.
        builder.HasIndex(b => new { b.Status, b.CreatedDate, b.Id })
            .HasDatabaseName("IX_ItemImportBatches_Status_CreatedDate_Id");

        builder.OwnsMany(b => b.History, history =>
        {
            history.ToTable("ItemImportBatchHistory");
            history.WithOwner().HasForeignKey("ItemImportBatchId");
            history.HasKey(h => h.Id);
            // Id is assigned app-side (Guid.CreateVersion7()). Without this, EF's default Guid-key
            // convention treats it as store-generated: when a new history entry is added to an
            // already-tracked batch (e.g. the Worker's process flow), the pre-populated key makes EF
            // infer an existing row and emit an UPDATE (0 rows -> DbUpdateConcurrencyException) instead
            // of an INSERT. ValueGeneratedNever() makes EF track added entries as Added.
            history.Property(h => h.Id).ValueGeneratedNever();
            history.Property(h => h.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            history.Property(h => h.Message).HasMaxLength(2000);
            history.HasIndex(h => h.CreatedAtUtc);
        });
    }
}
