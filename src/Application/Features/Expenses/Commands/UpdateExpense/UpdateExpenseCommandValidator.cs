using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Expenses.Commands.UpdateExpense;

public class UpdateExpenseCommandValidator(IApplicationDbContext dbContext)
    : ExpenseFieldsValidator<UpdateExpenseCommand>(dbContext);
