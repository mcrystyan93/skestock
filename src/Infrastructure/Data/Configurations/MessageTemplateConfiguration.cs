using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities.FruitDessert;
using skestock.Domain.Enums;

namespace skestock.Infrastructure.Data.Configurations;

public class MessageTemplateConfiguration : IEntityTypeConfiguration<MessageTemplate>
{
    // Fixed ids and instant keep the seed deterministic, so the model never reports pending changes.
    private static readonly DateTimeOffset SeedInstant = new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<MessageTemplate> builder)
    {
        builder.Property(t => t.Type).HasConversion<int>();
        builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Body).HasMaxLength(4000).IsRequired();

        // One active template per type; a new template is a new row.
        builder.HasIndex(t => t.Type)
            .IsUnique()
            .HasDatabaseName("UX_MessageTemplates_Type_Active")
            .HasFilter("[IsActive] = 1");

        builder.HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.LastModifiedBy)
            .WithMany()
            .HasForeignKey(t => t.LastModifiedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new MessageTemplate
            {
                Id = new Guid("0197c0de-0000-7000-8000-000000000001"),
                Type = FruitDessertMessageType.Reminder,
                Name = "Reminder fructe și desert",
                Body = "Bună ziua, {ContactPerson}! Vă reamintim că {Date} congregația {Congregation} aduce fructe și desert pentru {ClassName}. Vă mulțumim!",
                IsActive = true,
                CreatedDate = SeedInstant,
                LastModifiedDate = SeedInstant
            },
            new MessageTemplate
            {
                Id = new Guid("0197c0de-0000-7000-8000-000000000002"),
                Type = FruitDessertMessageType.Thanks,
                Name = "Mulțumire fructe și desert",
                Body = "Bună ziua, {ContactPerson}! Vă mulțumim că {Date} congregația {Congregation} a adus fructe și desert pentru {ClassName}. Dumnezeu să vă binecuvânteze!",
                IsActive = true,
                CreatedDate = SeedInstant,
                LastModifiedDate = SeedInstant
            });
    }
}
