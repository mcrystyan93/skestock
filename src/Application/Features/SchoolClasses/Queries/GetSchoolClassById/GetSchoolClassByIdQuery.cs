using skestock.Application.Features.SchoolClasses.Models;

namespace skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;

public class GetSchoolClassByIdQuery : IRequest<Result<SchoolClassDto>>
{
    public Guid Id { get; init; }
}
