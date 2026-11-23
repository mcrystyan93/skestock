using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Domain.Entities.Expenses;

namespace skestock.Application.Features.Expenses;

public interface IExpenseFields
{
    string? SupplierName { get; }
    string? DocumentNumber { get; }
    DateOnly? ExpenseDate { get; }
    string Currency { get; }
    decimal? TotalAmount { get; }
    decimal? VatAmount { get; }
    Guid? ExpenseCategoryId { get; }
}

// Rules shared by the manual create and update commands.
public abstract class ExpenseFieldsValidator<T> : AbstractValidator<T> where T : IExpenseFields
{
    protected ExpenseFieldsValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.SupplierName).MaximumLength(300);
        RuleFor(x => x.DocumentNumber).MaximumLength(100);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.TotalAmount).GreaterThanOrEqualTo(0).When(x => x.TotalAmount.HasValue);
        RuleFor(x => x.VatAmount).GreaterThanOrEqualTo(0).When(x => x.VatAmount.HasValue);
        RuleFor(x => x.VatAmount).LessThanOrEqualTo(x => x.TotalAmount!.Value)
            .When(x => x.VatAmount.HasValue && x.TotalAmount.HasValue);
        RuleFor(x => x.ExpenseDate!.Value)
            .InclusiveBetween(Expense.MinExpenseDate, Expense.MaxExpenseDate)
            .When(x => x.ExpenseDate.HasValue)
            .WithErrorCode(ValidationErrorCodes.Between);
        RuleFor(x => x.ExpenseCategoryId!.Value)
            .MustAsync((id, ct) => dbContext.ExpenseCategories.AsNoTracking().AnyAsync(c => c.Id == id, ct))
            .When(x => x.ExpenseCategoryId.HasValue)
            .WithMessage("Expense category does not exist")
            .WithErrorCode(ValidationErrorCodes.InvalidReference);
    }
}
