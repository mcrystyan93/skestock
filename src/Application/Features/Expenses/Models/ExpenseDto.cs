using skestock.Domain.Entities.Expenses;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Expenses.Models;

public class ExpenseDto
{
    public Guid Id { get; init; }
    public Guid SchoolClassId { get; init; }
    public ExpenseSource Source { get; init; }
    public ExpenseStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SupplierName { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? ExpenseDate { get; init; }
    public string Currency { get; init; } = Expense.DefaultCurrency;
    public decimal? TotalAmount { get; init; }
    public decimal? VatAmount { get; init; }
    public Guid? ExpenseCategoryId { get; init; }
    public string? ExpenseCategoryName { get; init; }
    public Guid? FileMetadataId { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }

    public static System.Linq.Expressions.Expression<Func<Expense, ExpenseDto>> Projection => e => new ExpenseDto
    {
        Id = e.Id,
        SchoolClassId = e.SchoolClassId,
        Source = e.Source,
        Status = e.Status,
        ErrorMessage = e.ErrorMessage,
        SupplierName = e.SupplierName,
        DocumentNumber = e.DocumentNumber,
        ExpenseDate = e.ExpenseDate,
        Currency = e.Currency,
        TotalAmount = e.TotalAmount,
        VatAmount = e.VatAmount,
        ExpenseCategoryId = e.ExpenseCategoryId,
        ExpenseCategoryName = e.ExpenseCategory != null ? e.ExpenseCategory.Name : null,
        FileMetadataId = e.FileMetadataId,
        UploadedAt = e.UploadedAt,
        ProcessedAt = e.ProcessedAt,
        CreatedDate = e.CreatedDate,
        LastModifiedDate = e.LastModifiedDate
    };
}
