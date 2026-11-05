using FluentResults;
using skestock.Application.Features.FruitDessertPlans.Abstractions;

namespace skestock.Infrastructure.FruitDessert;

// shortcut: Romanian numbers only (country code 40 + 9 digits); other countries are treated as invalid.
public class PhoneNumberNormalizer : IPhoneNumberNormalizer
{
    public Result<string> Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Result.Fail("Phone number is missing.");
        }

        var digits = new List<char>(phone.Length);
        foreach (var c in phone.Trim())
        {
            if (char.IsAsciiDigit(c))
            {
                digits.Add(c);
            }
            else if (!(c is ' ' or '-' or '.' or '(' or ')' || (c == '+' && digits.Count == 0)))
            {
                return Result.Fail("Phone number contains invalid characters.");
            }
        }

        var number = new string(digits.ToArray());
        if (number.StartsWith("00", StringComparison.Ordinal))
        {
            number = number[2..];
        }
        else if (number.StartsWith('0'))
        {
            number = "40" + number[1..];
        }

        return number.Length == 11 && number.StartsWith("40", StringComparison.Ordinal)
            ? Result.Ok(number)
            : Result.Fail("Phone number is not a valid Romanian number.");
    }
}
