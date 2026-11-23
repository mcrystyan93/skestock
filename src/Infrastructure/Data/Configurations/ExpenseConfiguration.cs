using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using skestock.Domain.Entities.Expenses;

namespace skestock.Infrastructure.Data.Configurations;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.SupplierName).HasMaxLength(Expense.MaxSupplierNameLength);
        builder.Property(e => e.DocumentNumber).HasMaxLength(Expense.MaxDocumentNumberLength);
        builder.Property(e => e.Currency).HasMaxLength(3).IsRequired();
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
        builder.Property(e => e.VatAmount).HasPrecision(18, 2);

        // Soft delete: deleted rows are invisible to every query unless IgnoreQueryFilters() is used.
        builder.HasQueryFilter(e => e.DeletedDate == null);

        builder.HasOne(e => e.SchoolClass)
            .WithMany()
            .HasForeignKey(e => e.SchoolClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ExpenseCategory)
            .WithMany()
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.FileMetadata)
            .WithMany()
            .HasForeignKey(e => e.FileMetadataId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UploadedByUser)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .HasPrincipalKey(u => u.IdentityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedBy)
            .WithMany()
            .HasForeignKey(e => e.CreatedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.LastModifiedBy)
            .WithMany()
            .HasForeignKey(e => e.LastModifiedById)
            .HasPrincipalKey(x => x.IdentityId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Listing (keyset by CreatedDate, Id) and the aggregation paths planned for statistics.
        builder.HasIndex(e => new { e.SchoolClassId, e.CreatedDate, e.Id }).HasDatabaseName("IX_Expenses_Class_CreatedDate_Id");
        builder.HasIndex(e => new { e.SchoolClassId, e.Status, e.CreatedDate, e.Id }).HasDatabaseName("IX_Expenses_Class_Status_CreatedDate_Id");
        builder.HasIndex(e => new { e.SchoolClassId, e.ExpenseDate, e.Id }).HasDatabaseName("IX_Expenses_Class_ExpenseDate_Id");
        builder.HasIndex(e => new { e.ExpenseCategoryId, e.ExpenseDate }).HasDatabaseName("IX_Expenses_Category_ExpenseDate");
        builder.HasIndex(e => new { e.SupplierName, e.ExpenseDate }).HasDatabaseName("IX_Expenses_Supplier_ExpenseDate");
    }
}
