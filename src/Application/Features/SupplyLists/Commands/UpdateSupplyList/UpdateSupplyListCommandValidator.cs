using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.SupplyLists.Commands.UpdateSupplyList;

public class UpdateSupplyListCommandValidator : SupplyListPayloadValidator<UpdateSupplyListCommand>
{
    public UpdateSupplyListCommandValidator(IApplicationDbContext dbContext)
        : base(dbContext, command => command.Id)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
