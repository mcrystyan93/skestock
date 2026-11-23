using NUnit.Framework;
using Shouldly;
using skestock.Domain.Entities.Expenses;
using skestock.Domain.Enums;
using skestock.Domain.Events.Expenses;

namespace skestock.Domain.UnitTests.Entities;

public class ExpenseTests
{
    private static Expense NewDocumentExpense() =>
        Expense.CreateFromDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    [Test]
    public void CreateFromDocument_StartsProcessingAndRaisesCreatedEvent()
    {
        var expense = NewDocumentExpense();

        expense.Status.ShouldBe(ExpenseStatus.Processing);
        expense.Currency.ShouldBe("RON");
        expense.DomainEvents.OfType<ExpenseCreatedEvent>().ShouldHaveSingleItem();
    }

    [Test]
    public void CreateManual_IsProcessedWithoutFile()
    {
        var expense = Expense.CreateManual(Guid.NewGuid(), Guid.NewGuid());

        expense.Status.ShouldBe(ExpenseStatus.Processed);
        expense.Source.ShouldBe(ExpenseSource.Manual);
        expense.FileMetadataId.ShouldBeNull();
    }

    [Test]
    public void ApplyExtractionResult_FillsFieldsKeepsRawJsonAndDefaultsCurrency()
    {
        var expense = NewDocumentExpense();

        expense.ApplyExtractionResult("{}", ExpenseSource.Invoice, "ACME", "F-1",
            new DateOnly(2026, 10, 1), null, 119m, 19m);

        expense.Status.ShouldBe(ExpenseStatus.Processed);
        expense.Currency.ShouldBe("RON");
        expense.ExtractedDataJson.ShouldBe("{}");
        expense.ProcessedAt.ShouldNotBeNull();
        expense.DomainEvents.OfType<ExpenseProcessedEvent>().ShouldHaveSingleItem();
    }

    [Test]
    public void MarkAsFailed_ThenUpdate_CompletesManuallyAndClearsError()
    {
        var expense = NewDocumentExpense();
        expense.MarkAsFailed("unreadable");
        expense.Status.ShouldBe(ExpenseStatus.Failed);

        expense.Update("Shop", null, null, "eur", 10m, null, null);

        expense.Status.ShouldBe(ExpenseStatus.Processed);
        expense.ErrorMessage.ShouldBeNull();
        expense.Currency.ShouldBe("EUR");
    }

    [Test]
    public void Retry_ReturnsFailedExpenseToProcessing()
    {
        var expense = NewDocumentExpense();
        expense.MarkAsFailed("boom");

        expense.Retry();

        expense.Status.ShouldBe(ExpenseStatus.Processing);
        expense.ErrorMessage.ShouldBeNull();
    }

    [Test]
    public void ApplyExtractionResult_ClampsOversizedAiValuesToColumnSizes()
    {
        var expense = NewDocumentExpense();

        expense.ApplyExtractionResult("{}", ExpenseSource.Receipt, new string('a', 500), new string('1', 200),
            null, "Romanian leu", 1m, null);

        expense.SupplierName!.Length.ShouldBe(Expense.MaxSupplierNameLength);
        expense.DocumentNumber!.Length.ShouldBe(Expense.MaxDocumentNumberLength);
        expense.Currency.ShouldBe("RON");
    }

    [TestCase(1999, 12, 31)]
    [TestCase(2101, 1, 1)]
    [TestCase(1, 1, 1)]
    public void ApplyExtractionResult_DropsDatesOutsideTheAcceptedRange(int year, int month, int day)
    {
        var expense = NewDocumentExpense();

        expense.ApplyExtractionResult("{}", ExpenseSource.Receipt, null, null,
            new DateOnly(year, month, day), null, 1m, null);

        expense.ExpenseDate.ShouldBeNull();
        expense.Status.ShouldBe(ExpenseStatus.Processed);
    }

    [TestCase(-1)]
    [TestCase(10_000_000_000_000_000)]
    public void ApplyExtractionResult_DropsAmountsTheColumnsCannotHold(double amount)
    {
        var expense = NewDocumentExpense();

        expense.ApplyExtractionResult("{}", ExpenseSource.Receipt, "ACME", "F-1",
            null, "RON", (decimal)amount, (decimal)amount);

        expense.Status.ShouldBe(ExpenseStatus.Processed);
        expense.TotalAmount.ShouldBeNull();
        expense.VatAmount.ShouldBeNull();
    }

    [Test]
    public void ApplyExtractionResult_KeepsAmountsAtTheBoundaries()
    {
        var expense = NewDocumentExpense();

        expense.ApplyExtractionResult("{}", ExpenseSource.Receipt, null, null,
            null, "RON", Expense.MaxAmount, 0m);

        expense.TotalAmount.ShouldBe(Expense.MaxAmount);
        expense.VatAmount.ShouldBe(0m);
    }
}
