using skestock.Application.Common.Errors;

namespace skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;

public class GetSchoolClassByIdQueryValidator : AbstractValidator<GetSchoolClassByIdQuery>
{
    public GetSchoolClassByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(ValidationErrorCodes.Required);
    }
}
