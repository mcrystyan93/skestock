using skestock.Domain.Entities.Stock;

namespace skestock.Domain.Enums;

/// <summary>
/// Predefined reasons staff can select when adjusting stock after a physical recount.
/// Mapped to <see cref="StockTransaction.Reason"/> (a free-text string column) via
/// <c>.ToString()</c> rather than adding a new enum column to that table.
/// </summary>
public enum AdjustmentReason
{
    Adjustment,     // manual correction
    Miscount,
    Damaged,
    Expired,
    Found,
    Other
}
