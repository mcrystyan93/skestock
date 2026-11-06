using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans.Abstractions;

public record MessageDispatch(string Url, FruitDessertMessageStatus Status);
