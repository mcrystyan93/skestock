using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Application.Common.Filtering;
using skestock.Domain.Entities.Congregations;

namespace skestock.Infrastructure.Data.Configurations;

public class CongregationConfiguration : IEntityTypeConfiguration<Congregation>
{
    public void Configure(EntityTypeBuilder<Congregation> builder)
    {
        // Name carries the accent-insensitive collation so search and the unique index below compare
        // names the same way the create/update validators do.
        builder.Property(c => c.Name).HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH).IsRequired()
            .UseCollation(TextSearchCollation.AccentInsensitive);
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Coordinator).HasMaxLength(100);
        builder.Property(c => c.CoordinatorPhone).HasMaxLength(30);
        builder.Property(c => c.DessertContactPerson).HasMaxLength(100);
        builder.Property(c => c.DessertContactPhone).HasMaxLength(30);
        builder.Property(c => c.Message).HasMaxLength(1000);

        // Soft delete: deleted rows are invisible to every query unless IgnoreQueryFilters() is used.
        builder.HasQueryFilter(c => c.DeletedDate == null);

        // Names are unique among active congregations only, so a deleted name can be reused.
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("UX_Congregations_Name")
            .HasFilter("[DeletedDate] IS NULL");

        // Composite indexes matching the (column, Id) tie-breaker pairs used by keyset pagination
        // in GetAllCongregationsHandler/CongregationSortConfiguration.
        builder.HasIndex(c => new { c.CreatedDate, c.Id }).HasDatabaseName("IX_Congregations_CreatedDate_Id");
        builder.HasIndex(c => new { c.LastModifiedDate, c.Id }).HasDatabaseName("IX_Congregations_LastModifiedDate_Id");
        builder.HasIndex(c => new { c.Name, c.Id }).HasDatabaseName("IX_Congregations_Name_Id");

        builder.HasOne(c => c.CreatedBy)
            .WithMany()
            .HasForeignKey(c => c.CreatedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.LastModifiedBy)
            .WithMany()
            .HasForeignKey(c => c.LastModifiedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
