using skestock.Domain.Entities.SchoolClasses;
using skestock.Domain.Entities.Storage;
using skestock.Domain.Entities.Users;
using skestock.Domain.Enums;
using skestock.Domain.Events.Expenses;

namespace skestock.Domain.Entities.Expenses;

public class Expense : BaseAuditableEntity, IKeysetEntity
{
    public const string DefaultCurrency = "RON";
    public const int MaxSupplierNameLength = 300;
    /// <summary>Largest value the decimal(18,2) amount columns hold.</summary>
    public const decimal MaxAmount = 9_999_999_999_999_999.99m;
    public const int MaxDocumentNumberLength = 100;

    public Guid SchoolClassId { get; set; }
    public SchoolClass SchoolClass { get; set; } = null!;

    public ExpenseSource Source { get; set; }
    public ExpenseStatus Status { get; set; }
    public string? ErrorMessage { get; set; } // populated when Status == Failed

    public string? SupplierName { get; set; }
    public string? DocumentNumber { get; set; }
    public DateOnly? ExpenseDate { get; set; }
    public string Currency { get; set; } = DefaultCurrency;
    public decimal? TotalAmount { get; set; } // includes VAT
    public decimal? VatAmount { get; set; }

    public Guid? ExpenseCategoryId { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }

    public Guid? FileMetadataId { get; set; } // null for manual expenses
    public FileMetadata? FileMetadata { get; set; }

    public Guid UploadedByUserId { get; set; }
    public UserProfile UploadedByUser { get; set; } = null!;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }

    // Raw AI output, kept for audit/comparison; the typed columns above are the editable data.
    public string? ExtractedDataJson { get; private set; }

    // Soft delete: null means active.
    public DateTimeOffset? DeletedDate { get; set; }

    public static Expense CreateFromDocument(Guid classId, Guid fileMetadataId, Guid uploadedByUserId)
    {
        var expense = new Expense
        {
            Id = Guid.CreateVersion7(),
            SchoolClassId = classId,
            Source = ExpenseSource.Receipt, // refined by the extraction result
            Status = ExpenseStatus.Processing,
            FileMetadataId = fileMetadataId,
            UploadedByUserId = uploadedByUserId
        };

        expense.AddDomainEvent(new ExpenseCreatedEvent(expense.Id, classId));
        return expense;
    }

    public static Expense CreateManual(Guid classId, Guid uploadedByUserId)
    {
        var expense = new Expense
        {
            Id = Guid.CreateVersion7(),
            SchoolClassId = classId,
            Source = ExpenseSource.Manual,
            Status = ExpenseStatus.Processed,
            UploadedByUserId = uploadedByUserId,
            ProcessedAt = DateTimeOffset.UtcNow
        };

        expense.AddDomainEvent(new ExpenseCreatedEvent(expense.Id, classId));
        return expense;
    }

    public void ApplyExtractionResult(
        string extractedDataJson,
        ExpenseSource source,
        string? supplierName,
        string? documentNumber,
        DateOnly? expenseDate,
        string? currency,
        decimal? totalAmount,
        decimal? vatAmount)
    {
        ExtractedDataJson = extractedDataJson;
        Source = source;
        // AI output is untrusted: clamp to the column sizes so a long value can't fail the save and loop in the queue.
        SupplierName = supplierName?.Trim() is { Length: > MaxSupplierNameLength } s ? s[..MaxSupplierNameLength] : supplierName?.Trim();
        DocumentNumber = documentNumber?.Trim() is { Length: > MaxDocumentNumberLength } d ? d[..MaxDocumentNumberLength] : documentNumber?.Trim();
        ExpenseDate = expenseDate;
        Currency = currency?.Trim() is { Length: 3 } c ? c.ToUpperInvariant() : DefaultCurrency;
        TotalAmount = ValidAmountOrNull(totalAmount);
        VatAmount = ValidAmountOrNull(vatAmount);
        ErrorMessage = null;
        Status = ExpenseStatus.Processed;
        ProcessedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new ExpenseProcessedEvent(Id, SchoolClassId));
    }

    // An amount the column cannot hold (or a negative one) is dropped for the user to fill in, not saved.
    private static decimal? ValidAmountOrNull(decimal? amount) =>
        amount is >= 0 and <= MaxAmount ? amount : null;

    public void MarkAsFailed(string errorMessage)
    {
        Status = ExpenseStatus.Failed;
        ErrorMessage = errorMessage;

        AddDomainEvent(new ExpenseFailedEvent(Id, SchoolClassId));
    }

    // Manual data entry; also completes an expense whose extraction failed.
    public void Update(
        string? supplierName,
        string? documentNumber,
        DateOnly? expenseDate,
        string currency,
        decimal? totalAmount,
        decimal? vatAmount,
        Guid? expenseCategoryId)
    {
        SupplierName = supplierName;
        DocumentNumber = documentNumber;
        ExpenseDate = expenseDate;
        Currency = currency.ToUpperInvariant();
        TotalAmount = totalAmount;
        VatAmount = vatAmount;
        ExpenseCategoryId = expenseCategoryId;

        if (Status == ExpenseStatus.Failed)
        {
            Status = ExpenseStatus.Processed;
            ErrorMessage = null;
            ProcessedAt ??= DateTimeOffset.UtcNow;
            AddDomainEvent(new ExpenseProcessedEvent(Id, SchoolClassId));
        }
    }

    public void Retry()
    {
        Status = ExpenseStatus.Processing;
        ErrorMessage = null;
    }
}
