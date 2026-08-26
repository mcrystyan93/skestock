using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.Property(i => i.Sku).HasMaxLength(50);
        builder.Property(i => i.Name).HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(DataSchemaConstants.DEFAULT_DESCRIPTION_LENGTH);
        builder.Property(i => i.Unit).HasMaxLength(20).IsRequired();

        builder.HasIndex(i => i.Sku).IsUnique();

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
