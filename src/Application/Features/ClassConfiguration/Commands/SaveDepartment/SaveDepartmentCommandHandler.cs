using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;

public sealed class SaveDepartmentCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SaveDepartmentCommand, Result<DepartmentTemplateDto>>
{
    public async ValueTask<Result<DepartmentTemplateDto>> Handle(SaveDepartmentCommand request, CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations
            .Include(item => item.DepartmentTemplates)
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);
        DepartmentTemplate department;
        if (request.Id is { } id)
        {
            var existing = configuration?.DepartmentTemplates.SingleOrDefault(item => item.Id == id);
            if (existing is null)
                return Result.Fail(new ClassConfigurationErrors.DepartmentNotFound(id));
            department = existing;
        }
        else
        {
            if (configuration is null)
            {
                configuration = new SharedClassConfiguration();
                dbContext.SharedClassConfigurations.Add(configuration);
            }
            department = new DepartmentTemplate();
            configuration.DepartmentTemplates.Add(department);
        }

        department.Name = request.Name.Trim();
        department.Responsibilities = request.Responsibilities.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok(new DepartmentTemplateDto(department.Id, department.Name, department.Responsibilities));
    }
}
