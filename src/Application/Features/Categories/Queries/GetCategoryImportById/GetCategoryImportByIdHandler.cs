using System.Text.Json;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Categories.Models;

namespace skestock.Application.Features.Categories.Queries.GetCategoryImportById;

public class GetCategoryImportByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCategoryImportByIdQuery, Result<CategoryImportReviewDto>>
{
    public async ValueTask<Result<CategoryImportReviewDto>> Handle(GetCategoryImportByIdQuery query,
        CancellationToken cancellationToken)
    {
        var import = await dbContext.CategoryImports
            .AsNoTracking()
            .Where(i => i.Id == query.Id)
            .Select(i => new
            {
                i.Id,
                i.Status,
                i.ExtractedDataJson,
                i.ErrorMessage,
                i.UploadedAt,
                i.ProcessedAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (import is null)
            return Result.Fail(new CategoryImportErrors.CategoryImportNotFound(query.Id));

        // No extraction yet (still Processing, or Failed before extraction) - return an empty,
        // suggestion-less review payload rather than failing, so the client can show status/context.
        var extraction = string.IsNullOrWhiteSpace(import.ExtractedDataJson)
            ? new CategoryExtractionResult()
            : JsonSerializer.Deserialize<CategoryExtractionResult>(import.ExtractedDataJson) ?? new CategoryExtractionResult();

        // De-duplicate the suggested names case-insensitively (the AI may repeat a name), preserving
        // first-seen order and casing.
        var suggestedNames = (extraction.Categories ?? [])
            .Select(c => c.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (suggestedNames.Count > 0)
        {
            var upperNames = suggestedNames.Select(n => n.ToUpperInvariant()).ToList();

            // Compare on ToUpper() on both sides (translatable to SQL) so the "already exists" flag is
            // case-insensitive regardless of the suggested name's casing.
            var matched = await dbContext.Categories
                .AsNoTracking()
                .Where(c => upperNames.Contains(c.Name.Trim().ToUpper()))
                .Select(c => c.Name)
                .ToListAsync(cancellationToken);

            foreach (var name in matched)
                existingNames.Add(name);
        }

        var suggestions = suggestedNames
            .Select(name => new CategoryImportReviewLineDto
            {
                Name = name,
                AlreadyExists = existingNames.Contains(name)
            })
            .ToList();

        var dto = new CategoryImportReviewDto
        {
            Id = import.Id,
            Status = import.Status,
            ErrorMessage = import.ErrorMessage,
            UploadedAt = import.UploadedAt,
            ProcessedAt = import.ProcessedAt,
            Suggestions = suggestions
        };

        return Result.Ok(dto);
    }
}
