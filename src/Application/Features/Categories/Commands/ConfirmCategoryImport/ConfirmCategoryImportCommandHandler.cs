using System.Text.Json;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Commands.ConfirmCategoryImport;

public class ConfirmCategoryImportCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ConfirmCategoryImportCommand, Result<CategoryImportConfirmationResultDto>>
{
    public async ValueTask<Result<CategoryImportConfirmationResultDto>> Handle(ConfirmCategoryImportCommand request,
        CancellationToken cancellationToken)
    {
        var import = await dbContext.CategoryImports
            .FirstOrDefaultAsync(i => i.Id == request.ImportId, cancellationToken);

        if (import is null)
            return Result.Fail(new CategoryImportErrors.CategoryImportNotFound(request.ImportId));

        // Idempotency for an accidental double-submit: if it's already confirmed, return the result
        // recorded at confirm time rather than creating categories again.
        if (import.Status == CategoryImportStatus.Confirmed && !string.IsNullOrWhiteSpace(import.ConfirmationResultJson))
        {
            var stored = JsonSerializer.Deserialize<CategoryImportConfirmationResultDto>(import.ConfirmationResultJson)
                         ?? new CategoryImportConfirmationResultDto { ImportId = import.Id, Status = import.Status };
            return Result.Ok(stored);
        }

        if (import.Status != CategoryImportStatus.PendingReview)
            return Result.Fail(new CategoryImportErrors.CategoryImportNotInReview(request.ImportId, import.Status));

        // Normalize: trim, drop blanks, and de-duplicate case-insensitively so "Dairy" and "dairy"
        // collapse to a single reviewed name.
        var reviewedNames = request.CategoryNames
            .Select(n => n.Trim())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resultCategories = await ResolveCategoriesAsync(reviewedNames, cancellationToken);

        var result = new CategoryImportConfirmationResultDto
        {
            ImportId = import.Id,
            Status = CategoryImportStatus.Confirmed,
            Categories = resultCategories
        };

        import.MarkAsConfirmed(JsonSerializer.Serialize(result));

        // Single SaveChanges wraps the newly created categories and the import status change in one
        // implicit transaction: all-or-nothing.
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Ok(result);
    }

    /// <summary>
    /// Matches each reviewed name against an existing category (case-insensitively) and creates the
    /// ones that don't exist yet. Only reviewed categories are created; the entities are attached (via
    /// <c>Add</c>) but persisted by the caller's SaveChanges.
    /// </summary>
    private async Task<List<CategoryImportResultCategoryDto>> ResolveCategoriesAsync(
        List<string> reviewedNames, CancellationToken cancellationToken)
    {
        var result = new List<CategoryImportResultCategoryDto>();

        if (reviewedNames.Count == 0)
            return result;

        var upperNames = reviewedNames.Select(n => n.ToUpperInvariant()).ToList();

        // Compare on ToUpper() on both sides (translatable to SQL) so category matching is
        // case-insensitive regardless of the reviewed name's casing.
        var existing = await dbContext.Categories
            .Where(c => upperNames.Contains(c.Name.Trim().ToUpper()))
            .ToListAsync(cancellationToken);

        var existingByName = existing.ToDictionary(c => c.Name.Trim().ToLowerInvariant(), c => c);

        foreach (var name in reviewedNames)
        {
            var key = name.ToLowerInvariant();
            if (existingByName.TryGetValue(key, out var match))
            {
                result.Add(new CategoryImportResultCategoryDto { Id = match.Id, Name = match.Name, Created = false });
                continue;
            }

            var category = new Category { Name = name };
            dbContext.Categories.Add(category);
            existingByName[key] = category;

            result.Add(new CategoryImportResultCategoryDto { Id = category.Id, Name = category.Name, Created = true });
        }

        return result;
    }
}
