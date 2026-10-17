using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;

public class UpdateSchoolClassCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateSchoolClassCommand, Result<SchoolClassDto>>
{
    public async ValueTask<Result<SchoolClassDto>> Handle(UpdateSchoolClassCommand request, CancellationToken cancellationToken)
    {
        var schoolClass = await dbContext.SchoolClasses
            .SingleOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (schoolClass is null)
            return Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.Id));

        // Closing moves the remaining stock, so it only happens through the close command. A class that stays
        // closed can still be renamed or re-dated, so this command deliberately does not require an open class.
        if ((request.Status == ClassStatus.Closed) != (schoolClass.Status == ClassStatus.Closed))
            return Result.Fail(new SchoolClassErrors.StatusChangeNotAllowed(request.Id));

        schoolClass.Name = request.Name.Trim();
        schoolClass.StartDate = request.StartDate;
        schoolClass.EndDate = request.EndDate;
        schoolClass.Status = request.Status;

        await dbContext.SaveChangesAsync(cancellationToken);

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
