using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Items.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Items.Commands.ConfirmItemImport;

public class ConfirmItemImportCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ConfirmItemImportCommand, Result<ItemImportConfirmationResultDto>>
{
    public async ValueTask<Result<ItemImportConfirmationResultDto>> Handle(ConfirmItemImportCommand request,
        CancellationToken cancellationToken)
    {
        var import = await dbContext.ItemImports
            .FirstOrDefaultAsync(i => i.Id == request.ImportId, cancellationToken);

        if (import is null)
            return Result.Fail(new ItemImportErrors.ItemImportNotFound(request.ImportId));

        // Idempotency for an accidental double-submit: if it's already confirmed, return the result
        // recorded at confirm time rather than creating items again.
        if (import.Status == ItemImportStatus.Confirmed && !string.IsNullOrWhiteSpace(import.ConfirmationResultJson))
        {
            var stored = JsonSerializer.Deserialize<ItemImportConfirmationResultDto>(import.ConfirmationResultJson)
                         ?? new ItemImportConfirmationResultDto { ImportId = import.Id, Status = import.Status };
            return Result.Ok(stored);
        }

        if (import.Status != ItemImportStatus.PendingReview)
            return Result.Fail(new ItemImportErrors.ItemImportNotInReview(request.ImportId, import.Status));

        // Normalize: trim every field and drop blanks, then de-duplicate by SKU (when present) or by
        // name+category, so repeated/near-duplicate suggestions collapse into a single item.
        if (request.Items.Any(item => string.IsNullOrWhiteSpace(item.CategoryName)))
            return Result.Fail(new ItemImportErrors.RowsWithoutCategory(request.ImportId));

        var reviewedItems = NormalizeAndDeduplicate(request.Items);

        var categoriesByName = await ResolveCategoriesAsync(reviewedItems, cancellationToken);
        var resultItems = await ResolveItemsAsync(reviewedItems, categoriesByName, cancellationToken);

        var result = new ItemImportConfirmationResultDto
        {
            ImportId = import.Id, Status = ItemImportStatus.Confirmed, Items = resultItems
        };

        import.MarkAsConfirmed(JsonSerializer.Serialize(result));

        // The concurrency stamp prevents two confirmations of the same import from creating rows twice.
        // SaveChanges still wraps all new rows and the import status change in one transaction.
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentImport = await dbContext.ItemImports
                .AsNoTracking()
                .SingleOrDefaultAsync(i => i.Id == import.Id, cancellationToken);

            if (currentImport?.Status == ItemImportStatus.Confirmed &&
                !string.IsNullOrWhiteSpace(currentImport.ConfirmationResultJson))
            {
                var stored = JsonSerializer.Deserialize<ItemImportConfirmationResultDto>(
                    currentImport.ConfirmationResultJson);
                if (stored is not null)
                    return Result.Ok(stored);
            }

            throw;
        }

        return Result.Ok(result);
    }

    private sealed record NormalizedItem(
        string? Sku,
        string Name,
        string CategoryName,
        string Unit,
        string? Description,
        bool IsPerishable);

    private static List<NormalizedItem> NormalizeAndDeduplicate(List<ConfirmItemImportItem> items)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<NormalizedItem>();

        foreach (var item in items)
        {
            var name = item.Name.Trim();
            var categoryName = item.CategoryName.Trim();

            if (name.Length == 0 || categoryName.Length == 0)
                continue;

            var sku = string.IsNullOrWhiteSpace(item.Sku) ? null : item.Sku.Trim();
            var unit = string.IsNullOrWhiteSpace(item.Unit) ? "unit" : item.Unit.Trim();
            var description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();

            var key = BuildKey(sku, name, categoryName);
            if (!seen.Add(key))
                continue;

            result.Add(new NormalizedItem(sku, name, categoryName, unit, description, item.IsPerishable));
        }

        return result;
    }

    // De-dup/match key: SKU when present (globally unique intent), otherwise name + category so two
    // different categories don't collapse into one item (mirrors
    // ConfirmGoodsReceiptImportCommandHandler.BuildNewItemKey).
    private static string BuildKey(string? sku, string name, string categoryName) =>
        !string.IsNullOrWhiteSpace(sku)
            ? $"sku:{sku.Trim().ToLowerInvariant()}"
            : $"name:{name.ToLowerInvariant()}|cat:{categoryName.ToLowerInvariant()}";

    /// <summary>
    /// Matches each reviewed item's category name against an existing category (case-insensitively)
    /// and creates the ones that don't exist yet. Only reviewed categories are created; the entities
    /// are attached (via <c>Add</c>) but persisted by the caller's SaveChanges.
    /// </summary>
    private async Task<Dictionary<string, (Category Category, bool Created)>> ResolveCategoriesAsync(
        List<NormalizedItem> reviewedItems, CancellationToken cancellationToken)
    {
        var existingSkus = reviewedItems
            .Where(i => i.Sku is not null)
            .Select(i => i.Sku!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var upperExistingSkus = existingSkus.Select(s => s.ToUpperInvariant()).ToList();

        var existingSkuValues = existingSkus.Count == 0
            ? []
            : await dbContext.Items
                .Where(i => i.Sku != null && upperExistingSkus.Contains(i.Sku.Trim().ToUpper()))
                .Select(i => i.Sku!)
                .ToListAsync(cancellationToken);

        var names = reviewedItems
            .Where(i => i.Sku is null || !existingSkuValues.Contains(i.Sku, StringComparer.OrdinalIgnoreCase))
            .Select(i => i.CategoryName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new Dictionary<string, (Category, bool)>(StringComparer.OrdinalIgnoreCase);

        if (names.Count == 0)
            return result;

        var upperNames = names.Select(n => n.ToUpperInvariant()).ToList();

        // Compare on ToUpper() on both sides (translatable to SQL) so category matching is
        // case-insensitive regardless of the reviewed name's casing.
        var existing = await dbContext.Categories
            .Where(c => upperNames.Contains(c.Name.Trim().ToUpper()))
            .ToListAsync(cancellationToken);

        foreach (var category in existing)
            result[category.Name.Trim()] = (category, false);

        foreach (var name in names)
        {
            if (result.ContainsKey(name))
                continue;

            var category = new Category { Name = name };
            dbContext.Categories.Add(category);
            result[name] = (category, true);
        }

        return result;
    }

    /// <summary>
    /// Matches each reviewed item against an existing item by SKU (case-insensitively) and reuses it
    /// instead of creating a duplicate; items without a SKU match (including items with no SKU at all)
    /// are created. The entities are attached (via <c>Add</c>) but persisted by the caller's
    /// SaveChanges.
    /// </summary>
    private async Task<List<ItemImportResultItemDto>> ResolveItemsAsync(
        List<NormalizedItem> reviewedItems,
        Dictionary<string, (Category Category, bool Created)> categoriesByName,
        CancellationToken cancellationToken)
    {
        var result = new List<ItemImportResultItemDto>();

        if (reviewedItems.Count == 0)
            return result;

        var skus = reviewedItems
            .Where(i => i.Sku is not null)
            .Select(i => i.Sku!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingBySku = new Dictionary<string, Item>(StringComparer.OrdinalIgnoreCase);

        if (skus.Count > 0)
        {
            var upperSkus = skus.Select(s => s.ToUpperInvariant()).ToList();

            // Compare on ToUpper() on both sides (translatable to SQL) so SKU matching is
            // case-insensitive regardless of the reviewed SKU's casing.
            var existing = await dbContext.Items
                .Include(i => i.Category)
                .Where(i => i.Sku != null && upperSkus.Contains(i.Sku.Trim().ToUpper()))
                .ToListAsync(cancellationToken);

            foreach (var item in existing)
                existingBySku[item.Sku!.Trim()] = item;
        }

        foreach (var reviewed in reviewedItems)
        {
            Item item;
            bool itemCreated;
            bool categoryCreated;

            if (reviewed.Sku is not null && existingBySku.TryGetValue(reviewed.Sku, out var existingItem))
            {
                item = existingItem;
                itemCreated = false;

                result.Add(new ItemImportResultItemDto
                {
                    Id = item.Id,
                    Sku = item.Sku,
                    Name = item.Name,
                    CategoryId = item.CategoryId,
                    CategoryName = item.Category.Name,
                    Created = false,
                    CategoryCreated = false
                });
                continue;
            }
            else
            {
                var resolvedCategory = categoriesByName[reviewed.CategoryName];
                var category = resolvedCategory.Category;
                categoryCreated = resolvedCategory.Created;
                item = new Item
                {
                    Sku = reviewed.Sku,
                    Name = reviewed.Name,
                    Description = reviewed.Description,
                    Unit = reviewed.Unit,
                    IsPerishable = reviewed.IsPerishable,
                    IsActive = true,
                    Category = category,
                    CategoryId = category.Id
                };

                dbContext.Items.Add(item);
                itemCreated = true;

                if (reviewed.Sku is not null)
                    existingBySku[reviewed.Sku] = item;
            }

            result.Add(new ItemImportResultItemDto
            {
                Id = item.Id,
                Sku = item.Sku,
                Name = item.Name,
                CategoryId = item.CategoryId,
                CategoryName = item.Category.Name,
                Created = itemCreated,
                CategoryCreated = categoryCreated
            });
        }

        return result;
    }
}
