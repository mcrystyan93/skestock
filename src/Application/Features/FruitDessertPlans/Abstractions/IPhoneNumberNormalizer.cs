namespace skestock.Application.Features.FruitDessertPlans.Abstractions;

public interface IPhoneNumberNormalizer
{
    /// <summary>Returns the international digits-only number (e.g. 40722123456) or a failure.</summary>
    Result<string> Normalize(string? phone);
}
