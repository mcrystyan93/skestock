using skestock.Domain.Entities;
using skestock.Domain.Entities.SupplyLists;

namespace skestock.Domain.Events.SupplyLists;

public class SupplyListDisabledEvent(SupplyList supplyList) : BaseEvent
{
    public SupplyList SupplyList { get; } = supplyList;
}
