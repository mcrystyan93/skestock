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
        builder.Property(configuration => configuration.Room4SeatCount).IsRequired().HasDefaultValue(0);
        builder.Property(configuration => configuration.Room2SeatCount)
            .IsRequired()
            .HasDefaultValue(0);
        builder.Property(configuration => configuration.Room6SeatCount).IsRequired().HasDefaultValue(0);
    }
}
