namespace skestock.Domain.Enums;

public enum OrderListStatus
{
    Draft,     // editable; lines can be added/updated/removed
    Submitted, // frozen; represents an order that has been placed
    Cancelled  // abandoned; kept for history, cannot be edited
}
