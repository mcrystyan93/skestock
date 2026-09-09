namespace skestock.Application.Documents.Interfaces;

public interface IItemDocumentExtractionService
{
    Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType,
        CancellationToken cancellationToken) where TResult : class;
}
