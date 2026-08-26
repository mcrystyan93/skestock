using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

    
public class CategoryConfiguration: IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH);

        // Composite indexes matching the (column, Id) tie-breaker pairs used by keyset pagination
        // in GetAllCategoriesQuery/CategorySortConfiguration, so the ORDER BY + WHERE > cursor
        // pattern can seek instead of scan for each supported sort.
        builder.HasIndex(c => new { c.CreatedDate, c.Id }).HasDatabaseName("IX_Categories_CreatedDate_Id");
        builder.HasIndex(c => new { c.LastModifiedDate, c.Id }).HasDatabaseName("IX_Categories_LastModifiedDate_Id");
        builder.HasIndex(c => new { c.Name, c.Id }).HasDatabaseName("IX_Categories_Name_Id");
        
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
