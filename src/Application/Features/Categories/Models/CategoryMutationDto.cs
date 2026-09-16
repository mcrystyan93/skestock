namespace skestock.Application.Features.Categories.Models;

public record CategoryMutationDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public CategoryIconDto? Icon { get; init; }
}
