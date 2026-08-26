namespace skestock.Domain.Enums;

public enum StockTransactionType
{
    Order,          // new batch arrives
    Usage,          // consumed / removed
    Adjustment,     // manual correction
    Transfer,       // moved between locations
    ExpiryWriteoff, // system-generated, batch expired
    Rollover        // system-generated, closing -> opening copy

}
