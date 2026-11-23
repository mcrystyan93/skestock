using skestock.Application.Common.Filtering;
using skestock.Application.Common.Models;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses.Models;

public static class ExpenseRequests
{
    public class GetAllExpensesRequest : BasePaginationFilter
    {
        public List<ColumnFilter> Filters { get; init; } = [];
    }

    public record CreateExpenseFromDocumentRequest(Guid ClassId, Guid FileMetadataId);

    // Body of POST (manual create) and PUT (update): the editable expense fields.
    public record ExpenseFieldsRequest(
        string? SupplierName,
        string? DocumentNumber,
        DateOnly? ExpenseDate,
        string? Currency,
        decimal? TotalAmount,
        decimal? VatAmount,
        Guid? ExpenseCategoryId);

    public record CreateExpenseRequest(Guid ClassId, ExpenseFieldsRequest Fields);

    public static string CurrencyOrDefault(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? Expense.DefaultCurrency : currency;
}
