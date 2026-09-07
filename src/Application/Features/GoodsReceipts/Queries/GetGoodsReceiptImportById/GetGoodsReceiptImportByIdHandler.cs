using System.Text.Json;
using skestock.Application.Common.Errors;
using skestock.Application.Common.Interfaces;
using skestock.Application.Features.GoodsReceipts.Models;

namespace skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptImportById;

public class GetGoodsReceiptImportByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGoodsReceiptImportByIdQuery, Result<GoodsReceiptImportReviewDto>>
{
    public async ValueTask<Result<GoodsReceiptImportReviewDto>> Handle(GetGoodsReceiptImportByIdQuery query,
        CancellationToken cancellationToken)
    {
        var import = await dbContext.GoodsReceiptImports
            .AsNoTracking()
            .Where(i => i.Id == query.Id)
            .Select(i => new
            {
                i.Id,
                i.Status,
                i.ClassId,
                ClassName = i.Class.Name,
                i.ExtractedDataJson
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (import is null)
            return Result.Fail(new GoodsReceiptImportErrors.GoodsReceiptImportNotFound(query.Id));

        // No extraction yet (still Processing, or Failed before extraction) - return an empty,
        // line-less review payload rather than failing, so the client can show status/context.
        var extraction = string.IsNullOrWhiteSpace(import.ExtractedDataJson)
            ? new GoodsReceiptExtractionResult()
            : JsonSerializer.Deserialize<GoodsReceiptExtractionResult>(import.ExtractedDataJson) ?? new GoodsReceiptExtractionResult();

        // Batch the catalog match: one query for every distinct product code across all lines,
        // matched against active items by SKU (case-insensitive), then a dictionary lookup per line.
        var productCodes = extraction.LineItems
            .Select(l => l.ProductCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!.Trim().ToUpper())
            .Distinct()
            .ToList();

        var matchesByCode = new Dictionary<string, GoodsReceiptImportReviewMatchDto>(StringComparer.OrdinalIgnoreCase);

        if (productCodes.Count > 0)
        {
            // Compare on ToUpper() on both sides (translatable to SQL) so SKU matching is
            // case-insensitive regardless of the extracted product code's casing.
            var matchedItems = await dbContext.Items
                .AsNoTracking()
                .Where(item => item.IsActive && item.Sku != null && productCodes.Contains(item.Sku.ToUpper()))
                .Select(item => new GoodsReceiptImportReviewMatchDto
                {
                    Id = item.Id,
                    Sku = item.Sku,
                    Name = item.Name,
                    Unit = item.Unit,
                    IsPerishable = item.IsPerishable,
                    CategoryId = item.CategoryId,
                    CategoryName = item.Category.Name
                })
                .ToListAsync(cancellationToken);

            // First match wins if two active items somehow share a SKU (SKU uniqueness isn't
            // enforced at the DB level for nullable values).
            foreach (var match in matchedItems)
                matchesByCode.TryAdd(match.Sku!, match);
        }

        var lines = extraction.LineItems
            .Select(line => new GoodsReceiptImportReviewLineDto
            {
                RawItemText = line.Name,
                ProductCode = line.ProductCode,
                Name = line.Name,
                Unit = line.Unit,
                Category = line.Category,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                IsPerishable = line.IsPerishable,
                MatchedItem = !string.IsNullOrWhiteSpace(line.ProductCode)
                              && matchesByCode.TryGetValue(line.ProductCode.Trim(), out var match)
                    ? match
                    : null
            })
            .ToList();

        var dto = new GoodsReceiptImportReviewDto
        {
            Id = import.Id,
            Status = import.Status,
            ClassId = import.ClassId,
            ClassName = import.ClassName,
            SupplierReference = extraction.SupplierReference,
            ReceivedAt = extraction.ReceivedAt,
            Lines = lines
        };

        return Result.Ok(dto);
    }
}
