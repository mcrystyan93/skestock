using skestock.Application.Common.Errors;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetDepartment;

public sealed class GetDepartmentQueryValidator : AbstractValidator<GetDepartmentQuery>
{
    public GetDepartmentQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty().WithErrorCode(ValidationErrorCodes.Required);
    }
}
