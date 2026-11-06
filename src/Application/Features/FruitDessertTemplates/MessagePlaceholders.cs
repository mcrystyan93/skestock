namespace skestock.Application.Features.FruitDessertTemplates;

public sealed record MessagePlaceholder(string Token, string Label, string Example, int MaxLength);

// Single source for the renderer's known set, the save validator and the GET response.
// MaxLength mirrors the column limits (Application can't reference Infrastructure's DataSchemaConstants).
public static class MessagePlaceholders
{
    public static readonly IReadOnlyList<MessagePlaceholder> All =
    [
        // Congregation.DessertContactPerson is HasMaxLength(100).
        new("{ContactPerson}", "Persoana de contact", "Maria Popescu", 100),
        // Longest output of the renderer's date format: "miercuri, 30 septembrie 2026".
        new("{Date}", "Data", "luni, 9 noiembrie 2026", 28),
        // Congregation.Name and SchoolClass.Name are HasMaxLength(DEFAULT_NAME_LENGTH = 100).
        new("{Congregation}", "Congregația", "Congregația Nord", 100),
        new("{ClassName}", "Clasa", "Toamna 2026", 100)
    ];

    public static bool IsKnown(string token) => All.Any(p => p.Token == token);
}
