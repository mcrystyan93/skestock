using skestock.Application.Features.SupplyLists.Models;
using skestock.Domain.Enums;

namespace skestock.Application.Features.SupplyLists;

// Shape shared by the create/update commands so they can share validation.
public interface ISupplyListPayload
{
    string Name { get; }
    string? Note { get; }
    SupplyListFrequency Frequency { get; }
    int? IntervalWeeks { get; }
    List<SupplyListLineInput> Lines { get; }
}
