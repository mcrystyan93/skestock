using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Common.Models;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceipt;
using skestock.Application.Features.GoodsReceipts.Commands.CreateGoodsReceiptImport;
using skestock.Application.Features.GoodsReceipts.Models;
using skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceipts;
using skestock.Application.Features.GoodsReceipts.Queries.GetAllGoodsReceiptImports;
using skestock.Application.Features.GoodsReceipts.Queries.GetGoodsReceiptById;

namespace skestock.Web.Endpoints;

public class GoodsReceipts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(GetAllGoodsReceipts, "get-all");
        groupBuilder.MapGet(GetGoodsReceiptById, "{id}");
        groupBuilder.MapPost(CreateGoodsReceipt, "");
        groupBuilder.MapPost(CreateGoodsReceiptImport, "imports");
        groupBuilder.MapPost(GetAllGoodsReceiptImports, "imports/get-all");
    }

    [EndpointSummary("Get all goods receipts")]
    [EndpointDescription("Retrieves a paginated, filterable list of goods receipts.")]
    public static async Task<Results<Ok<PaginatedResponse<GoodsReceiptListItemDto>>, ProblemHttpResult>> GetAllGoodsReceipts(
        ISender sender, GoodsReceiptRequests.GetAllGoodsReceiptsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllGoodsReceiptsQuery
        {
            Filters = request.Filters,
            Sort = request.Sort,
            Cursor = request.Cursor,
            SearchTerm = request.SearchTerm,
            PageSize = request.PageSize
        };

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Get a goods receipt by id")]
    [EndpointDescription("Retrieves a single goods receipt, including its lines, by its id.")]
    public static async Task<Results<Ok<GoodsReceiptDto>, ProblemHttpResult>> GetGoodsReceiptById(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetGoodsReceiptByIdQuery { Id = id }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Create a new goods receipt")]
    [EndpointDescription("Records a goods receipt: creates one stock batch and one order-type stock transaction per line, all in a single transaction.")]
    public static async Task<Results<Created<GoodsReceiptDto>, ProblemHttpResult>> CreateGoodsReceipt(
        ISender sender, GoodsReceiptRequests.CreateGoodsReceiptRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateGoodsReceiptCommand
        {
            ClassId = request.ClassId,
            SupplierReference = request.SupplierReference,
            Note = request.Note,
            Lines = request.Lines.Select(l => new CreateGoodsReceiptLine
            {
                ItemId = l.ItemId,
                LocationId = l.LocationId,
                Quantity = l.Quantity,
                ExpiryDate = l.ExpiryDate,
                UnitPrice = l.UnitPrice
            }).ToList()
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/api/GoodsReceipts/{result.Value.Id}", result.Value);
    }

    [EndpointSummary("Create a new goods receipt import")]
    [EndpointDescription("Starts a goods receipt import for an uploaded file: creates a GoodsReceiptImport in the Processing state, capturing the blob path from the referenced file metadata.")]
    public static async Task<Results<Created<GoodsReceiptImportDto>, ProblemHttpResult>> CreateGoodsReceiptImport(
        ISender sender, GoodsReceiptRequests.CreateGoodsReceiptImportRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateGoodsReceiptImportCommand
        {
            ClassId = request.ClassId,
            FileMetadataId = request.FileMetadataId
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Created($"/api/GoodsReceipts/imports/{result.Value.Id}", result.Value);
    }

    [EndpointSummary("Get all goods receipt imports")]
    [EndpointDescription("Retrieves a paginated, filterable list of goods receipt imports. Filter by classId, status, etc. via the Filters column-filter list.")]
    public static async Task<Results<Ok<PaginatedResponse<GoodsReceiptImportListItemDto>>, ProblemHttpResult>> GetAllGoodsReceiptImports(
        ISender sender, GoodsReceiptRequests.GetAllGoodsReceiptImportsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAllGoodsReceiptImportsQuery
        {
            Filters = request.Filters,
            Sort = request.Sort,
            Cursor = request.Cursor,
            SearchTerm = request.SearchTerm,
            PageSize = request.PageSize
        };

        var result = await sender.Send(query, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }
}
