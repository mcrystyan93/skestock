using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities.FruitDessert;

namespace skestock.Infrastructure.Data.Configurations;

public class FruitDessertPlanEntryConfiguration : IEntityTypeConfiguration<FruitDessertPlanEntry>
{
    public void Configure(EntityTypeBuilder<FruitDessertPlanEntry> builder)
    {
        // Guards idempotent generation of days against concurrent first listings.
        builder.HasIndex(e => new { e.SchoolClassId, e.Date })
            .IsUnique()
            .HasDatabaseName("UX_FruitDessertPlanEntries_SchoolClassId_Date");
        builder.HasIndex(e => e.CongregationId).HasDatabaseName("IX_FruitDessertPlanEntries_CongregationId");

        builder.HasOne(e => e.SchoolClass)
            .WithMany()
            .HasForeignKey(e => e.SchoolClassId)
            .OnDelete(DeleteBehavior.Cascade);

        // Congregations are soft deleted, so a hard restrict never blocks normal use.
        builder.HasOne(e => e.Congregation)
            .WithMany()
            .HasForeignKey(e => e.CongregationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LastModifiedBy)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
