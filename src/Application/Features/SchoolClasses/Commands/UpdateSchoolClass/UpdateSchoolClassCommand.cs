using skestock.Application.Common.Caching;
using skestock.Application.Features.SchoolClasses.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses.Commands.UpdateSchoolClass;

public class UpdateSchoolClassCommand : IRequest<Result<SchoolClassDto>>, ICacheInvalidation
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public ClassStatus Status { get; init; }

    // Invalidate every cached GetAllSchoolClasses page/filter/sort combination - an edited school
    // class can affect any of them (default sort, search matches, filters, etc.).
    public IReadOnlyCollection<string> Tags => [CacheConstants.SchoolClassListTag];
}
