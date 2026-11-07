namespace skestock.Application.Features.FruitDessertPlans.Abstractions;

public interface IInformarePdfRenderer
{
    /// <summary>Renders a single-column A4 PDF with a bold title and a wrapped body.</summary>
    byte[] Render(string title, string body);
}
