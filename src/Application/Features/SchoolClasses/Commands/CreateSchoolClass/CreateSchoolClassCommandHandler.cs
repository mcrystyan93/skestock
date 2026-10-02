using skestock.Application.Common.Interfaces;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Domain.Entities;
using skestock.Application.Common.Errors;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.SchoolClasses.Commands.CreateSchoolClass;

public class CreateSchoolClassCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateSchoolClassCommand, Result<SchoolClassDto>>
{
    public async ValueTask<Result<SchoolClassDto>> Handle(CreateSchoolClassCommand request,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations
            .Include(item => item.DepartmentTemplates)
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);

        if (configuration is null)
            return Result.Fail(new SchoolClassErrors.ConfigurationRequired());

        var schoolClass = new SchoolClass
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            InvitationCount = configuration.InvitationCount,
            Departments = configuration.DepartmentTemplates.Select(template => new ClassDepartment
            {
                Id = Guid.CreateVersion7(), Name = template.Name, Responsibilities = template.Responsibilities
            }).ToList()
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
            InvitationCount = schoolClass.InvitationCount,
            IsConfigurationInitialized = schoolClass.InvitationCount.HasValue,
            Departments = schoolClass.Departments.Select(department => new ClassDepartmentDto(
                department.Id, department.Name, department.Responsibilities, department.ResponsiblePerson)).ToArray(),
            CreatedByName = schoolClass.CreatedBy?.FullName,
            LastModifiedByName = schoolClass.LastModifiedBy?.FullName,
            CreatedDate = schoolClass.CreatedDate,
            LastModifiedDate = schoolClass.LastModifiedDate
        });
    }
}
