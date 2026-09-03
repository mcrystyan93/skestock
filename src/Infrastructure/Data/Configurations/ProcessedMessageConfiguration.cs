using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Queues;

namespace skestock.Infrastructure.Data.Configurations;

public class ProcessedMessageConfiguration: IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(x => x.Id); // Id is the message's own primary key - unique constraint
    }
}
