namespace skestock.Application.Features.OrderLists.Models;

// Presentation-ready snapshot handed to the Excel exporter. All grouping/ordering is
// resolved in the Application handler so the renderer stays a pure layout concern.
public record OrderListExportModel
{
    public string? Name { get; init; }
    public string? Note { get; init; }
    public string? ClassName { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public IReadOnlyList<OrderListExportGroup> Groups { get; init; } = [];
}

public record OrderListExportGroup
{
    public required string CategoryName { get; init; }
    public IReadOnlyList<OrderListExportLine> Lines { get; init; } = [];
}

public record OrderListExportLine
{
    public required string ProductName { get; init; }
    public decimal Quantity { get; init; }
    public string? Unit { get; init; }
    public string? Notes { get; init; }
}
