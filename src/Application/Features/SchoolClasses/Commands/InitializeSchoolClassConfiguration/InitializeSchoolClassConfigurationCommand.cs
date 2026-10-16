using skestock.Application.Common.Behaviours;
using skestock.Application.Common.Caching;

namespace skestock.Application.Features.SchoolClasses.Commands.InitializeSchoolClassConfiguration;

public sealed class InitializeSchoolClassConfigurationCommand : IRequest<Result>, ICacheInvalidation, IRequiresOpenClass
{
    Guid? IRequiresOpenClass.ClassId => SchoolClassId;

    public Guid SchoolClassId { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.SchoolClassListTag];
}
