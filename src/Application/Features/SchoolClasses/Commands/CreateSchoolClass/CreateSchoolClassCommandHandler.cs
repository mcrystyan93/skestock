using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;

public class CreateSchoolClassCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateSchoolClassCommand, Result<SchoolClassDto>>
{
    public async ValueTask<Result<SchoolClassDto>> Handle(CreateSchoolClassCommand request, CancellationToken cancellationToken)
    {
        var schoolClass = new SchoolClass
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status
        };

        dbContext.SchoolClasses.Add(schoolClass);
        await dbContext.SaveChangesAsync(cancellationToken);

        // CreatedBy/LastModifiedBy navigations aren't loaded on a freshly-inserted entity (only
        // the *Id FKs are set by AuditableEntityInterceptor), so the *Name fields are null here by
        // design - callers needing the name can re-fetch via GetAllSchoolClasses.
        return Result.Ok(new SchoolClassDto
        {
            Id = schoolClass.Id,
            Name = schoolClass.Name,
            StartDate = schoolClass.StartDate,
            EndDate = schoolClass.EndDate,
            Status = schoolClass.Status,
            CreatedByName = schoolClass.CreatedBy?.FullName,
            LastModifiedByName = schoolClass.LastModifiedBy?.FullName,
            CreatedDate = schoolClass.CreatedDate,
            LastModifiedDate = schoolClass.LastModifiedDate
        });
    }
}
