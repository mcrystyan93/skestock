using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SupplyLists;

namespace skestock.Infrastructure.Data.Configurations;

public class SupplyListConfiguration : IEntityTypeConfiguration<SupplyList>
{
    public void Configure(EntityTypeBuilder<SupplyList> builder)
    {
        builder.Property(l => l.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(l => l.Name)
            .IsUnique();

        builder.Property(l => l.Note)
            .HasMaxLength(1000);

        builder.Property(l => l.Frequency)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_SupplyLists_IntervalWeeks",
            "([Frequency] = 'EveryXWeeks' AND [IntervalWeeks] BETWEEN 2 AND 52) " +
            "OR ([Frequency] <> 'EveryXWeeks' AND [IntervalWeeks] IS NULL)"));

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
