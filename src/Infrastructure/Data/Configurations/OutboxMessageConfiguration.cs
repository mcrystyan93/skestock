using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Application.Queues;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration: IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.TraceParent).HasMaxLength(MessagingTelemetry.TraceParentMaxLength);
        builder.Property(x => x.TraceState).HasMaxLength(MessagingTelemetry.TraceStateMaxLength);

        // critical for the poller's query performance
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.ClaimedUntilUtc, x.CreatedAtUtc })
            .HasFilter("[ProcessedAtUtc] IS NULL"); // SQL Server filtered index; adjust for your provider
    }
}
