using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Security;
using skestock.Application.Features.Expenses.Models;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses.Commands.UpdateExpense;

[Authorize]
public class UpdateExpenseCommand : IRequest<Result<ExpenseDto>>, IRequiresOpenClass, IExpenseFields
{
    Guid? IRequiresOpenClass.ExpenseId => Id;

    public Guid Id { get; init; }
    public string? SupplierName { get; init; }
    public string? DocumentNumber { get; init; }
    public DateOnly? ExpenseDate { get; init; }
    public string Currency { get; init; } = Expense.DefaultCurrency;
    public decimal? TotalAmount { get; init; }
    public decimal? VatAmount { get; init; }
    public Guid? ExpenseCategoryId { get; init; }
}
