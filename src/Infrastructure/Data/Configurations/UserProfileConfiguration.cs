using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;
using skestock.Infrastructure.Identity;

namespace skestock.Infrastructure.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.IdentityId)
            .IsRequired(); // Matches AspNetUsers.Id string length

        builder.HasIndex(u => u.IdentityId)
            .IsUnique();

        // Configure 1:1 relationship with AspNetUsers
        builder.HasOne<ApplicationUser>()
            .WithOne(au => au.UserProfile)
            .HasForeignKey<UserProfile>(u => u.IdentityId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(u => u.FirstName)
            .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasComputedColumnSql("CONCAT([FirstName], ' ',  [LastName])", stored: true);
        
        // UserProfile has two self-referencing FKs (CreatedBy/LastModifiedBy, both -> UserProfile)
        // inherited from BaseAuditableEntity. Without explicit configuration EF Core cannot
        // determine which navigation pairs with which foreign key, and throws:
        // "The dependent side could not be determined for the one-to-one relationship
        // between 'UserProfile.CreatedBy' and 'UserProfile.LastModifiedBy'".
        // Configure both as independent many-to-one relationships with no inverse navigation,
        // and disable cascading deletes to avoid multiple cascade paths on a self-referencing table.
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
