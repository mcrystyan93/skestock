using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetDepartments;

public sealed class GetDepartmentsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDepartmentsQuery, Result<IReadOnlyList<DepartmentTemplateDto>>>
{
    public async ValueTask<Result<IReadOnlyList<DepartmentTemplateDto>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = await dbContext.SharedClassConfigurations.AsNoTracking()
            .Where(configuration => configuration.Id == SharedClassConfiguration.SingletonId)
            .SelectMany(configuration => configuration.DepartmentTemplates)
            .OrderBy(department => department.Name).ThenBy(department => department.Id)
            .Select(department => new DepartmentTemplateDto(department.Id, department.Name, department.Responsibilities))
            .ToListAsync(cancellationToken);
        return Result.Ok<IReadOnlyList<DepartmentTemplateDto>>(departments);
    }
}
