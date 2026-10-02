using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Infrastructure.Data.Configurations;

public class SharedClassConfigurationConfiguration : IEntityTypeConfiguration<SharedClassConfiguration>
{
    public void Configure(EntityTypeBuilder<SharedClassConfiguration> builder)
    {
        builder.Property(configuration => configuration.Id).ValueGeneratedNever();
        builder.Property(configuration => configuration.InvitationCount).IsRequired();
    }
}
