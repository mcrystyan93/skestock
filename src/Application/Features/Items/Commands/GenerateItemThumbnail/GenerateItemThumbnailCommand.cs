namespace skestock.Application.Features.Items.Commands.GenerateItemThumbnail;

public record GenerateItemThumbnailCommand(Guid ItemId, Guid ImageFileId) : IRequest<Result>;
