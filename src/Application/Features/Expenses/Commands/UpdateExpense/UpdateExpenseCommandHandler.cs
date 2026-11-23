using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Expenses.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Expenses.Commands.UpdateExpense;

public class UpdateExpenseCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateExpenseCommand, Result<ExpenseDto>>
{
    public async ValueTask<Result<ExpenseDto>> Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
        if (expense is null)
            return Result.Fail(new ExpenseErrors.ExpenseNotFound(request.Id));

        // A late AI result would overwrite whatever the user typed meanwhile.
        if (expense.Status == ExpenseStatus.Processing)
            return Result.Fail(new ExpenseErrors.ExpenseBeingProcessed(expense.Id));

        expense.Update(request.SupplierName, request.DocumentNumber, request.ExpenseDate, request.Currency,
            request.TotalAmount, request.VatAmount, request.ExpenseCategoryId);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(await dbContext.Expenses.AsNoTracking()
            .Where(e => e.Id == expense.Id)
            .Select(ExpenseDto.Projection)
            .SingleAsync(cancellationToken));
    }
}
