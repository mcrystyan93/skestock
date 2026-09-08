namespace skestock.Application.Documents.Interfaces;

public interface IStockDocumentExtractionService
{
    Task<TResult> ExtractAsync<TResult>(Stream stream, string mimeType,
        CancellationToken cancellationToken) where TResult: class;
}
