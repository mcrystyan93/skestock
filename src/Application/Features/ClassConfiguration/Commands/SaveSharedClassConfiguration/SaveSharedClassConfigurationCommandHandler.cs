using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveSharedClassConfiguration;

public sealed class SaveSharedClassConfigurationCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SaveSharedClassConfigurationCommand, Result<SharedClassConfigurationDto>>
{
    public async ValueTask<Result<SharedClassConfigurationDto>> Handle(
        SaveSharedClassConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations
            .Include(item => item.DepartmentTemplates)
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);

        if (configuration is null)
        {
            configuration = new SharedClassConfiguration { InvitationCount = request.InvitationCount };
            dbContext.SharedClassConfigurations.Add(configuration);
        }
        else
        {
            configuration.InvitationCount = request.InvitationCount;
        }

        var existingDepartments = configuration.DepartmentTemplates.ToDictionary(item => item.Id);
        var retainedIds = new HashSet<Guid>();
        var updatedDepartments = new List<DepartmentTemplate>(request.Departments.Count);

        foreach (var input in request.Departments)
        {
            DepartmentTemplate department;
            if (input.Id is { } id)
            {
                if (!existingDepartments.TryGetValue(id, out department!))
                {
                    return Result.Fail("Departamentul selectat nu mai există în configurare.");
                }

                retainedIds.Add(id);
            }
            else
            {
                department = new DepartmentTemplate();
            }

            department.Name = input.Name.Trim();
            department.Responsibilities = input.Responsibilities.Trim();
            updatedDepartments.Add(department);
        }

        foreach (var removed in configuration.DepartmentTemplates.Where(item => !retainedIds.Contains(item.Id))
                     .ToList())
        {
            configuration.DepartmentTemplates.Remove(removed);
        }

        configuration.DepartmentTemplates = updatedDepartments;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(new SharedClassConfigurationDto(
            true,
            configuration.InvitationCount,
            true,
            updatedDepartments.Select(item => new DepartmentTemplateDto(item.Id, item.Name, item.Responsibilities))
                .ToList()));
    }
}
