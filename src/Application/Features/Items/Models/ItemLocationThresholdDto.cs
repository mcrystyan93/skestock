namespace skestock.Application.Features.Items.Models;

public record ItemLocationThresholdDto
{
    public Guid LocationId { get; init; }
    public int MinThreshold { get; init; }
}
