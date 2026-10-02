using skestock.Application.Common.Caching;

namespace skestock.Application.Features.SchoolClasses.Commands.InitializeSchoolClassConfiguration;

public sealed class InitializeSchoolClassConfigurationCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid SchoolClassId { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.SchoolClassListTag];
}
