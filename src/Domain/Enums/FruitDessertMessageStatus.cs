namespace skestock.Domain.Enums;

// Persisted as int: only append new values. Sent/Delivered/Failed are reserved for a future Cloud API sender.
public enum FruitDessertMessageStatus
{
    Opened = 1,
    Sent = 2,
    Delivered = 3,
    Failed = 4
}
