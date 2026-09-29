using skestock.Domain.Entities;

namespace skestock.Domain.Events.SupplyLists;

public class SupplyListDisabledEvent(SupplyList supplyList) : BaseEvent
{
    public SupplyList SupplyList { get; } = supplyList;
}
