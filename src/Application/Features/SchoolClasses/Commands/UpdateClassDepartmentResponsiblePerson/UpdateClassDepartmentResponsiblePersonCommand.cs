using skestock.Application.Common.Caching;

namespace skestock.Application.Features.SchoolClasses.Commands.UpdateClassDepartmentResponsiblePerson;

public sealed class UpdateClassDepartmentResponsiblePersonCommand : IRequest<Result>, ICacheInvalidation
{
    public Guid SchoolClassId { get; init; }
    public Guid DepartmentId { get; init; }
    public string? ResponsiblePerson { get; init; }

    public IReadOnlyCollection<string> Tags => [CacheConstants.SchoolClassListTag];
}
