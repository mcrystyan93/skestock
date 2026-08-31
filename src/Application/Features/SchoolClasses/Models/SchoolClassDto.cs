using skestock.Domain.Enums;

namespace skestock.Application.Features.SchoolClasses.Models;

public record SchoolClassDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public ClassStatus Status { get; init; }
    public string? CreatedByName { get; init; }
    public string? LastModifiedByName { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset LastModifiedDate { get; init; }
}

public record SchoolClassSummary(int Id, int NoOfGoodsReceipt, decimal TotalAmount, int LowStockItemsCount);
