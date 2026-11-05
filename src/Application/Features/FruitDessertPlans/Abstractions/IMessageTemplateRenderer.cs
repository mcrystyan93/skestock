namespace skestock.Application.Features.FruitDessertPlans.Abstractions;

public interface IMessageTemplateRenderer
{
    /// <summary>Replaces the known placeholders; fails with <c>UnknownPlaceholder</c> instead of sending corrupt text.</summary>
    Result<string> Render(string template, MessageTemplateValues values);
}
