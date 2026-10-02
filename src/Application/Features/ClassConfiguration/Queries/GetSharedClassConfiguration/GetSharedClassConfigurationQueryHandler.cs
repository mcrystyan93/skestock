using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Constants;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetSharedClassConfiguration;

public sealed class GetSharedClassConfigurationQueryHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<GetSharedClassConfigurationQuery, Result<SharedClassConfigurationDto>>
{
    public async ValueTask<Result<SharedClassConfigurationDto>> Handle(
        GetSharedClassConfigurationQuery request,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations
            .AsNoTracking()
            .Include(item => item.DepartmentTemplates)
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);

        var canManage = user.Roles?.Contains(Roles.Administrator, StringComparer.Ordinal) is true;
        return Result.Ok(new SharedClassConfigurationDto(
            configuration is not null,
            configuration?.InvitationCount ?? 0,
            canManage,
            configuration?.DepartmentTemplates
                .OrderBy(department => department.Name)
                .Select(department =>
                    new DepartmentTemplateDto(department.Id, department.Name, department.Responsibilities))
                .ToArray() ?? []));
    }
}
