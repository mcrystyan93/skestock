using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;

namespace skestock.Application.Features.SchoolClasses.Commands.UpdateClassDepartmentResponsiblePerson;

public sealed class UpdateClassDepartmentResponsiblePersonCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateClassDepartmentResponsiblePersonCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateClassDepartmentResponsiblePersonCommand request,
        CancellationToken cancellationToken)
    {
        var department = await dbContext.SchoolClasses
            .Where(schoolClass => schoolClass.Id == request.SchoolClassId)
            .SelectMany(schoolClass => schoolClass.Departments)
            .SingleOrDefaultAsync(department => department.Id == request.DepartmentId, cancellationToken);

        if (department is null)
        {
            var classExists = await dbContext.SchoolClasses
                .AnyAsync(schoolClass => schoolClass.Id == request.SchoolClassId, cancellationToken);
            return classExists
                ? Result.Fail(new SchoolClassErrors.ClassDepartmentNotFound(request.SchoolClassId, request.DepartmentId))
                : Result.Fail(new SchoolClassErrors.SchoolClassNotFound(request.SchoolClassId));
        }

        department.ResponsiblePerson = string.IsNullOrWhiteSpace(request.ResponsiblePerson)
            ? null
            : request.ResponsiblePerson.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
