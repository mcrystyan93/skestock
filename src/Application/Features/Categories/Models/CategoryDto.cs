namespace skestock.Application.Features.Categories.Models;

public record CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}
