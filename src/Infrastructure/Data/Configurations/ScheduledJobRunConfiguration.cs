using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public sealed class ScheduledJobRunConfiguration : IEntityTypeConfiguration<ScheduledJobRun>
{
    public void Configure(EntityTypeBuilder<ScheduledJobRun> builder)
    {
        builder.HasKey(run => run.JobName);
        builder.Property(run => run.JobName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(run => run.Status)
            .IsRequired();
        builder.Property(run => run.AttemptCount)
            .IsRequired();
        builder.Property(run => run.LastError)
            .HasMaxLength(4000);
    }
}
