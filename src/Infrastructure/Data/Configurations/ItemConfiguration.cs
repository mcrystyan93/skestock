using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        // Accent-insensitive collation lives on the columns that back text search (Name/Sku), so the
        // listing query can search them without wrapping each row in EF.Functions.Collate(...).
        builder.Property(i => i.Sku).HasMaxLength(50).UseCollation(TextSearchCollation.AccentInsensitive);
        builder.Property(i => i.Name).HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH).IsRequired()
            .UseCollation(TextSearchCollation.AccentInsensitive);
        builder.Property(i => i.Description).HasMaxLength(DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH);
        builder.Property(i => i.Unit).HasMaxLength(20).IsRequired();

        // SKU is optional; SQL Server's unfiltered unique index would allow only one SKU-less item.
        // This filtered unique index also serves the "sku" keyset sort.
        builder.HasIndex(i => i.Sku)
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL");

        // Default keyset sort: (CreatedDate DESC, Id DESC).
        builder.HasIndex(i => new { i.CreatedDate, i.Id })
            .HasDatabaseName("IX_Items_CreatedDate_Id")
            .IsDescending();

        // "lastModifiedDate" keyset sort.
        builder.HasIndex(i => new { i.LastModifiedDate, i.Id })
            .HasDatabaseName("IX_Items_LastModifiedDate_Id")
            .IsDescending();

        // "name" keyset sort (ascending index also seeks descending scans).
        builder.HasIndex(i => new { i.Name, i.Id })
            .HasDatabaseName("IX_Items_Name_Id");

        // Common "active items, newest first" listing path.
        builder.HasIndex(i => new { i.IsActive, i.CreatedDate, i.Id })
            .HasDatabaseName("IX_Items_IsActive_CreatedDate_Id")
            .IsDescending(false, true, true);

        // Category-filtered listing sorted by default order; leading column also covers the FK,
        // so EF suppresses the redundant single-column CategoryId index.
        builder.HasIndex(i => new { i.CategoryId, i.CreatedDate, i.Id })
            .HasDatabaseName("IX_Items_CategoryId_CreatedDate_Id")
            .IsDescending(false, true, true);

        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
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
