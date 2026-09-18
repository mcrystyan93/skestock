using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.OrderLists.Commands.CreateOrderList;

public class CreateOrderListCommandValidator : AbstractValidator<CreateOrderListCommand>
{
    private const int NameMaxLength = 200;
    private const int NoteMaxLength = 1000;

    public CreateOrderListCommandValidator(IApplicationDbContext dbContext)
    {
        RuleFor(x => x.ClassId)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required)
            .MustAsync((classId, ct) => ClassExistsAsync(dbContext, classId, ct))
            .WithMessage("The referenced school class does not exist")
            .WithErrorCode(ValidationErrorCodes.InvalidReference);

        RuleFor(x => x.Name)
            .MaximumLength(NameMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        RuleFor(x => x.Note)
            .MaximumLength(NoteMaxLength)
            .WithErrorCode(ValidationErrorCodes.MaxLength);

        this.AddOrderListLineRules(x => x.Lines, dbContext);
    }

    private static async Task<bool> ClassExistsAsync(IApplicationDbContext dbContext, Guid classId, CancellationToken cancellationToken)
    {
        return await dbContext.SchoolClasses
            .AsNoTracking()
            .AnyAsync(c => c.Id == classId, cancellationToken);
    }
}
