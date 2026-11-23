using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Security;
using skestock.Application.Features.Expenses.Models;

namespace skestock.Application.Features.Expenses.Commands.CreateExpenseFromDocument;

// Requires an authenticated user: Expense.UploadedByUserId is required and distinct from the CreatedBy audit column.
[Authorize]
public class CreateExpenseFromDocumentCommand : IRequest<Result<ExpenseDto>>, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => ClassId;

    public Guid ClassId { get; init; }
    public Guid FileMetadataId { get; init; }
}
