using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Expenses.Models;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<CreateExpenseCommand, Result<ExpenseDto>>
{
    public async ValueTask<Result<ExpenseDto>> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        var identityId = Guard.Against.Null(user.Id, message: "Creating an expense requires an authenticated user.");

        var expense = Expense.CreateManual(request.ClassId, identityId);
        expense.Update(request.SupplierName, request.DocumentNumber, request.ExpenseDate, request.Currency,
            request.TotalAmount, request.VatAmount, request.ExpenseCategoryId);

        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await dbContext.Expenses.AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(ExpenseDto.Projection)
            .SingleAsync(cancellationToken));
    }
}
