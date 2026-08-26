using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Models;

namespace skestock.Application.Features.SchoolClasses.Queries.GetSchoolClassById;

public class GetSchoolClassByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetSchoolClassByIdQuery, Result<SchoolClassDto>>
{
    public async ValueTask<Result<SchoolClassDto>> Handle(GetSchoolClassByIdQuery request, CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.SchoolClasses
            .AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new SchoolClassDto
            {
                Id = c.Id,
                Name = c.Name,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                CreatedByName = c.CreatedBy != null ? c.CreatedBy.FullName : null,
                LastModifiedByName = c.LastModifiedBy != null ? c.LastModifiedBy.FullName : null,
                CreatedDate = c.CreatedDate,
                LastModifiedDate = c.LastModifiedDate
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (schoolClass is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.Id));

        return Result.Ok(schoolClass);
    }
}
