using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Security;
using skestock.Application.Features.Expenses.Models;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses.Commands.CreateExpense;

// Manual expense, entered from scratch (no document).
[Authorize]
public class CreateExpenseCommand : IRequest<Result<ExpenseDto>>, IRequiresOpenClass, IExpenseFields
{
    Guid? IRequiresOpenClass.ClassId => ClassId;

    public Guid ClassId { get; init; }
    public string? SupplierName { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? ExpenseDate { get; init; }
    public string Currency { get; init; } = Expense.DefaultCurrency;
    public decimal? TotalAmount { get; init; }
    public decimal? VatAmount { get; init; }
    public Guid? ExpenseCategoryId { get; init; }
}
