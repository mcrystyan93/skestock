using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Constants;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;

[Authorize(Roles = Roles.Administrator)]
public sealed class SaveDepartmentCommand : IRequest<Result<DepartmentTemplateDto>>, ICacheInvalidation
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Responsibilities { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.DepartmentListTag];
}
