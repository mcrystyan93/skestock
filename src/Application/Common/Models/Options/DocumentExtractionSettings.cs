namespace skestock.Application.Common.Models.Options;

public class DocumentExtractionSettings
{
    public ExtractionProvider Provider { get; init; } = ExtractionProvider.OpenAI;
}
