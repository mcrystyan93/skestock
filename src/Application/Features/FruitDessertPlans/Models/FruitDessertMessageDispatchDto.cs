using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans.Models;

public record FruitDessertMessageDispatchDto(
    string Url,
    FruitDessertMessageType Type,
    FruitDessertMessageStatus Status,
    DateTimeOffset SentAt);
