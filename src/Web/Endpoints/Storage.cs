using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Storage.Commands.ConfirmUpload;
using skestock.Application.Storage.Commands.RequestUpload;
using skestock.Application.Storage.DTOs;
using skestock.Application.Storage.Models;

namespace skestock.Web.Endpoints;

public class Storage : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(RequestUpload, "request-upload").RequireAuthorization();
        groupBuilder.MapPost(ConfirmUpload, "confirm-upload").RequireAuthorization();
    }

    [EndpointSummary("Request a file upload")]
    [EndpointDescription("Registers pending file metadata and returns a short-lived SAS URL the client uploads the blob to.")]
    public static async Task<Results<Ok<UploadRequestResult>, ProblemHttpResult>> RequestUpload(
        ISender sender, StorageRequests.RequestUploadRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestUploadCommand
        {
            FileName = request.FileName,
            ContentType = request.ContentType
        };

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(result.Value);
    }

    [EndpointSummary("Confirm a file upload")]
    [EndpointDescription("Verifies the uploaded blob exists, records its size/ETag, and marks the file metadata as completed.")]
    public static async Task<Results<Ok<FileMetadataDto>, ProblemHttpResult>> ConfirmUpload(
        ISender sender, StorageRequests.ConfirmUploadRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ConfirmUploadCommand { FileId = request.FileId }, cancellationToken);

        if (result.IsFailed)
            return result.ToProblemHttpResult();

        return TypedResults.Ok(FileMetadataDto.FromEntity(result.Value));
    }
}
