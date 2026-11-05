using FluentResults;
using System.Text.RegularExpressions;
using skestock.Application.Common.Errors;
using skestock.Application.Features.FruitDessertPlans.Abstractions;

namespace skestock.Infrastructure.FruitDessert;

public partial class MessageTemplateRenderer : IMessageTemplateRenderer
{
    // Hand-written names instead of CultureInfo so the output does not depend on ICU being available.
    private static readonly string[] DayNames = ["duminică", "luni", "marți", "miercuri", "joi", "vineri", "sâmbătă"];

    private static readonly string[] MonthNames =
    [
        "ianuarie", "februarie", "martie", "aprilie", "mai", "iunie",
        "iulie", "august", "septembrie", "octombrie", "noiembrie", "decembrie"
    ];

    public Result<string> Render(string template, MessageTemplateValues values)
    {
        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{ContactPerson}"] = values.ContactPerson,
            ["{Date}"] = FormatDate(values.Date),
            ["{Congregation}"] = values.Congregation,
            ["{ClassName}"] = values.ClassName
        };

        var unknown = PlaceholderRegex().Matches(template)
            .Select(m => m.Value)
            .Where(p => !replacements.ContainsKey(p))
            .Distinct()
            .ToList();
        if (unknown.Count > 0)
        {
            return Result.Fail(new FruitDessertErrors.UnknownPlaceholder(unknown));
        }

        return Result.Ok(PlaceholderRegex().Replace(template, m => replacements[m.Value]));
    }

    private static string FormatDate(DateOnly date) =>
        $"{DayNames[(int)date.DayOfWeek]}, {date.Day} {MonthNames[date.Month - 1]} {date.Year}";

    [GeneratedRegex(@"\{[A-Za-z]+\}")]
    private static partial Regex PlaceholderRegex();
}
