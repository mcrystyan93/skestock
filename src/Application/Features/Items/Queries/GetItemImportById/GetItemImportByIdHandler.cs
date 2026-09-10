using System.Text.Json;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Documents.Models;
using skestock.Application.Features.Items.Models;

namespace skestock.Application.Features.Items.Queries.GetItemImportById;

public class GetItemImportByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetItemImportByIdQuery, Result<ItemImportReviewDto>>
{
    public async ValueTask<Result<ItemImportReviewDto>> Handle(GetItemImportByIdQuery query,
        CancellationToken cancellationToken)
    {
        var import = await dbContext.ItemImports
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
            return Result.Fail(new ItemImportErrors.ItemImportNotFound(query.Id));

        // No extraction yet (still Processing, or Failed before extraction) - return an empty,
        // suggestion-less review payload rather than failing, so the client can show status/context.
        var extraction = string.IsNullOrWhiteSpace(import.ExtractedDataJson)
            ? new ItemExtractionResult()
            : JsonSerializer.Deserialize<ItemExtractionResult>(import.ExtractedDataJson) ?? new ItemExtractionResult();

        // De-duplicate the suggested items by SKU (when present) or name+category (case-insensitive),
        // preserving first-seen order and casing - mirrors ConfirmItemImportCommandHandler's key.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var suggestedItems = new List<ExtractedItem>();

        foreach (var item in extraction.Items ?? [])
        {
            var name = item.Name?.Trim();
            var categoryName = item.CategoryName?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                continue;

            var sku = string.IsNullOrWhiteSpace(item.Sku) ? null : item.Sku.Trim();
            var normalizedCategoryName = categoryName ?? string.Empty;
            var key = sku is not null
                ? $"sku:{sku.ToLowerInvariant()}"
                : $"name:{name.ToLowerInvariant()}|cat:{normalizedCategoryName.ToLowerInvariant()}";

            if (!seen.Add(key))
                continue;

            suggestedItems.Add(new ExtractedItem
            {
                Sku = sku,
                Name = name,
                CategoryName = normalizedCategoryName,
                Unit = string.IsNullOrWhiteSpace(item.Unit) ? "unit" : item.Unit.Trim(),
                Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                IsPerishable = item.IsPerishable,
            });
        }

        var skus = suggestedItems.Where(i => i.Sku is not null).Select(i => i.Sku!).ToList();
        var matchedCategoriesByName = new Dictionary<string, ItemImportReviewCategoryDto>(
            StringComparer.OrdinalIgnoreCase);
        var categoryNames = suggestedItems.Select(i => i.CategoryName).Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (categoryNames.Count > 0)
        {
            var upperNames = categoryNames.Select(n => n.ToUpperInvariant()).ToList();

            // Compare on ToUpper() on both sides (translatable to SQL) so the "already exists" flag is
            // case-insensitive regardless of the suggested category name's casing.
            var matchedCategories = await dbContext.Categories
                .AsNoTracking()
                .Where(c => upperNames.Contains(c.Name.Trim().ToUpper()))
                .Select(c => new ItemImportReviewCategoryDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync(cancellationToken);

            foreach (var category in matchedCategories)
                matchedCategoriesByName.TryAdd(category.Name.Trim(), category);
        }

        var existingItems = new List<ItemImportReviewItemDto>();
        var itemNames = suggestedItems.Select(i => i.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (itemNames.Count > 0)
        {
            var upperItemNames = itemNames.Select(n => n.ToUpperInvariant()).ToList();
            var upperSkus = skus.Select(s => s.ToUpperInvariant()).ToList();
            existingItems = await dbContext.Items
                .AsNoTracking()
                .Where(i => upperItemNames.Contains(i.Name.Trim().ToUpper()) ||
                            (i.Sku != null && upperSkus.Contains(i.Sku.Trim().ToUpper())))
                .Select(i => new ItemImportReviewItemDto
                {
                    Id = i.Id,
                    Sku = i.Sku,
                    Name = i.Name,
                    Description = i.Description,
                    Unit = i.Unit,
                    IsPerishable = i.IsPerishable,
                    CategoryId = i.CategoryId,
                    CategoryName = i.Category.Name
                })
                .ToListAsync(cancellationToken);
        }

        var matchedItemsBySku = existingItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Sku))
            .GroupBy(i => i.Sku!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var suggestions = suggestedItems
            .Select(item =>
            {
                matchedCategoriesByName.TryGetValue(item.CategoryName, out var matchedCategory);

                ItemImportReviewItemDto? matchedItem = null;
                if (item.Sku is not null)
                    matchedItemsBySku.TryGetValue(item.Sku, out matchedItem);

                if (matchedItem is null && matchedCategory is not null)
                {
                    var fallbackMatches = existingItems
                        .Where(existing => existing.CategoryId == matchedCategory.Id &&
                                            string.Equals(
                                                Normalize(existing.Name),
                                                Normalize(item.Name),
                                                StringComparison.Ordinal))
                        .ToList();

                    if (fallbackMatches.Count == 1)
                        matchedItem = fallbackMatches[0];
                }

                return new ItemImportReviewLineDto
                {
                    Sku = item.Sku,
                    Name = item.Name,
                    CategoryName = item.CategoryName,
                    Unit = item.Unit ?? "unit",
                    Description = item.Description,
                    IsPerishable = item.IsPerishable,
                    ItemAlreadyExists = matchedItem is not null,
                    CategoryAlreadyExists = matchedCategory is not null,
                    MatchedCategory = matchedCategory,
                    MatchedItem = matchedItem
                };
            })
            .ToList();

        var dto = new ItemImportReviewDto
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

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
