namespace skestock.Application.Features.Categories.Models;

public record CategoryMutationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public CategoryIconDto? Icon { get; init; }
}
