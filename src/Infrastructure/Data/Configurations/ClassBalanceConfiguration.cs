using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class ClassBalanceConfiguration : IEntityTypeConfiguration<ClassBalance>
{
    public void Configure(EntityTypeBuilder<ClassBalance> builder)
    {
        // Restrict on all FKs to avoid multiple cascade paths from Item/Location/SchoolClass
        // being referenced by several dependent entities (see StockBatchConfiguration).
        builder.HasOne(cb => cb.Class)
            .WithMany(c => c.Balances)
            .HasForeignKey(cb => cb.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cb => cb.Item)
            .WithMany(i => i.ClassBalances)
            .HasForeignKey(cb => cb.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cb => cb.Location)
            .WithMany(l => l.ClassBalances)
            .HasForeignKey(cb => cb.LocationId)
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
