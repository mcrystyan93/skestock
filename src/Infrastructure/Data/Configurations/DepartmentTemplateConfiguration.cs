using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;

namespace skestock.Infrastructure.Data.Configurations;

public class DepartmentTemplateConfiguration : IEntityTypeConfiguration<DepartmentTemplate>
{
    public void Configure(EntityTypeBuilder<DepartmentTemplate> builder)
    {
        builder.Property(template => template.Name)
            .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
            .IsRequired();
        builder.Property(template => template.Responsibilities).IsRequired();
        builder.HasOne(template => template.SharedClassConfiguration)
            .WithMany(configuration => configuration.DepartmentTemplates)
            .HasForeignKey(template => template.SharedClassConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
