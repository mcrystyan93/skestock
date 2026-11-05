using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities.FruitDessert;

namespace skestock.Infrastructure.Data.Configurations;

public class FruitDessertMessageConfiguration : IEntityTypeConfiguration<FruitDessertMessage>
{
    public void Configure(EntityTypeBuilder<FruitDessertMessage> builder)
    {
        builder.Property(m => m.Type).HasConversion<int>();
        builder.Property(m => m.Status).HasConversion<int>();
        builder.Property(m => m.RecipientPhone).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Body).HasMaxLength(2000).IsRequired();

        builder.HasIndex(m => new { m.EntryId, m.Type }).HasDatabaseName("IX_FruitDessertMessages_EntryId_Type");
        builder.HasIndex(m => m.TemplateId).HasDatabaseName("IX_FruitDessertMessages_TemplateId");

        builder.HasOne(m => m.Entry)
            .WithMany(e => e.Messages)
            .HasForeignKey(m => m.EntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Template)
            .WithMany()
            .HasForeignKey(m => m.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.CreatedBy)
            .WithMany()
            .HasForeignKey(m => m.CreatedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.LastModifiedBy)
            .WithMany()
            .HasForeignKey(m => m.LastModifiedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
