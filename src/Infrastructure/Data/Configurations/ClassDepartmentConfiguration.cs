using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Infrastructure.Data.Configurations;

public class ClassDepartmentConfiguration : IEntityTypeConfiguration<ClassDepartment>
{
    public void Configure(EntityTypeBuilder<ClassDepartment> builder)
    {
        builder.Property(department => department.Name)
            .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
            .IsRequired();
        builder.Property(department => department.Responsibilities).IsRequired();
        builder.HasOne(department => department.SchoolClass)
            .WithMany(schoolClass => schoolClass.Departments)
            .HasForeignKey(department => department.SchoolClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
