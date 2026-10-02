namespace skestock.Application.Features.ClassConfiguration.Models;

public sealed record SharedClassConfigurationDto(
    bool IsConfigured,
    int InvitationCount,
    bool CanManage,
    IReadOnlyList<DepartmentTemplateDto> Departments);

public sealed record DepartmentTemplateDto(Guid Id, string Name, string Responsibilities);
