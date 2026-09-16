using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.Categories.Models;
using skestock.Domain.Entities;
using skestock.Domain.Enums;

namespace skestock.Application.Features.Categories.Commands.ConfirmCategoryImportBatch;

public class ConfirmCategoryImportBatchCommandHandler(IApplicationDbContext dbContext, IUser user)
    : IRequestHandler<ConfirmCategoryImportBatchCommand, Result<CategoryImportBatchConfirmationResponseDto>>
{
    public async ValueTask<Result<CategoryImportBatchConfirmationResponseDto>> Handle(
        ConfirmCategoryImportBatchCommand request, CancellationToken cancellationToken)
    {
        if (user.Id is not { } identityId)
            return Result.Fail(new CategoryImportBatchErrors.CategoryImportBatchNotFound(request.BatchId));

        var batch = await dbContext.CategoryImportBatches
            .Include(importBatch => importBatch.History)
            .FirstOrDefaultAsync(importBatch =>
                importBatch.Id == request.BatchId && importBatch.UploadedByUserId == identityId,
                cancellationToken);

        if (batch is null)
            return Result.Fail(new CategoryImportBatchErrors.CategoryImportBatchNotFound(request.BatchId));

        if (batch.Status == CategoryImportBatchStatus.Confirmed &&
            !string.IsNullOrWhiteSpace(batch.ConfirmationResultJson))
        {
            var stored = JsonSerializer.Deserialize<CategoryImportBatchConfirmationResultDto>(
                batch.ConfirmationResultJson);
            return Result.Ok(ToResponse(stored ?? new CategoryImportBatchConfirmationResultDto
            {
                BatchId = batch.Id,
                Status = batch.Status
            }));
        }

        if (batch.Status != CategoryImportBatchStatus.PendingReview)
            return Result.Fail(new CategoryImportBatchErrors.CategoryImportBatchNotInReview(
                request.BatchId, batch.Status));

        var reviewedNames = request.CategoryNames
            .Select(name => name.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resultCategories = await ResolveCategoriesAsync(reviewedNames, cancellationToken);
        var result = new CategoryImportBatchConfirmationResultDto
        {
            BatchId = batch.Id,
            Status = CategoryImportBatchStatus.Confirmed,
            Categories = resultCategories
        };

        batch.MarkAsConfirmed(JsonSerializer.Serialize(result));
        batch.History.Add(ImportBatchHistory.Confirmed());

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            var currentBatch = await dbContext.CategoryImportBatches
                .AsNoTracking()
                .SingleOrDefaultAsync(importBatch =>
                    importBatch.Id == batch.Id && importBatch.UploadedByUserId == identityId,
                    cancellationToken);
            if (currentBatch?.Status == CategoryImportBatchStatus.Confirmed &&
                !string.IsNullOrWhiteSpace(currentBatch.ConfirmationResultJson))
            {
                var stored = JsonSerializer.Deserialize<CategoryImportBatchConfirmationResultDto>(
                    currentBatch.ConfirmationResultJson);
                if (stored is not null)
                    return Result.Ok(ToResponse(stored));
            }

            throw;
        }

        return Result.Ok(ToResponse(result));
    }

    private static CategoryImportBatchConfirmationResponseDto ToResponse(
        CategoryImportBatchConfirmationResultDto result)
    {
        return new CategoryImportBatchConfirmationResponseDto
        {
            BatchId = result.BatchId,
            Status = result.Status
        };
    }

    private async Task<List<CategoryImportBatchResultCategoryDto>> ResolveCategoriesAsync(
        List<string> reviewedNames, CancellationToken cancellationToken)
    {
        var result = new List<CategoryImportBatchResultCategoryDto>();
        if (reviewedNames.Count == 0)
            return result;

        var upperNames = reviewedNames.Select(name => name.ToUpperInvariant()).ToList();
        var existingQuery = dbContext.Categories.AsQueryable();
        existingQuery = TextSearchCollation.IsSqlServer(dbContext.Database)
            ? existingQuery.Where(category => upperNames.Contains(
                EF.Functions.Collate(category.Name.Trim(), TextSearchCollation.AccentInsensitive)))
            : existingQuery.Where(category => upperNames.Contains(category.Name.Trim().ToUpper()));
        var existing = await existingQuery.ToListAsync(cancellationToken);
        var existingByName = existing.ToDictionary(
            category => category.Name.Trim().ToLowerInvariant(), category => category);

        foreach (var name in reviewedNames)
        {
            var key = name.ToLowerInvariant();
            if (existingByName.TryGetValue(key, out var match))
            {
                result.Add(new CategoryImportBatchResultCategoryDto
                {
                    Id = match.Id,
                    Name = match.Name,
                    Created = false
                });
                continue;
            }

            var category = new Category { Name = name };
            dbContext.Categories.Add(category);
            existingByName[key] = category;
            result.Add(new CategoryImportBatchResultCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Created = true
            });
        }

        return result;
    }
}
