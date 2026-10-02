using skestock.Application.Common.Caching;
using skestock.Application.Common.Security;
using skestock.Domain.Constants;

namespace skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;

[Authorize(Roles = Roles.Administrator)]
public sealed class DeleteDepartmentCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public IReadOnlyCollection<string> Tags => [CacheConstants.ConfigurationTag, CacheConstants.DepartmentListTag];
}
