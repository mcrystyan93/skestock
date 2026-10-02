using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Constants;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveSharedClassConfiguration;

[Authorize(Roles = Roles.Administrator)]
public sealed class SaveSharedClassConfigurationCommand : IRequest<Result<SharedClassConfigurationDto>>
{
    public int InvitationCount { get; init; }
    public IReadOnlyCollection<DepartmentTemplateInput> Departments { get; init; } = [];
}

public sealed record DepartmentTemplateInput(Guid? Id, string Name, string Responsibilities);
