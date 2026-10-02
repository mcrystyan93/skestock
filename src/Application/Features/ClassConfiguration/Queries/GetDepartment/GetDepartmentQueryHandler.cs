using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetDepartment;

public sealed class GetDepartmentQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDepartmentQuery, Result<DepartmentTemplateDto>>
{
    public async ValueTask<Result<DepartmentTemplateDto>> Handle(GetDepartmentQuery request, CancellationToken cancellationToken)
    {
        var department = await dbContext.SharedClassConfigurations.AsNoTracking()
            .Where(configuration => configuration.Id == SharedClassConfiguration.SingletonId)
            .SelectMany(configuration => configuration.DepartmentTemplates)
            .Where(department => department.Id == request.Id)
            .Select(department => new DepartmentTemplateDto(department.Id, department.Name, department.Responsibilities))
            .SingleOrDefaultAsync(cancellationToken);
        return department is null
            ? Result.Fail<DepartmentTemplateDto>(new ClassConfigurationErrors.DepartmentNotFound(request.Id))
            : Result.Ok(department);
    }
}
