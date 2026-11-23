using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandValidator : ExpenseFieldsValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator(IApplicationDbContext dbContext) : base(dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .DependentRules(() =>
            {
                RuleFor(x => x.ClassId)
                    .MustAsync((classId, ct) => dbContext.SchoolClasses.AsNoTracking().AnyAsync(c => c.Id == classId, ct))
                    .WithMessage("School class does not exist")
                    .WithErrorCode(ValidationErrorCodes.InvalidReference);
            });
    }
}
