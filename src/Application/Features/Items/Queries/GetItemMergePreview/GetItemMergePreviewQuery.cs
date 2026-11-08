using skestock.Application.Common.Security;
using skestock.Domain.Constants;

namespace skestock.Application.Features.Items.Queries.GetItemMergePreview;

/// <summary>Not cacheable: it must be exact when the admin confirms the merge.</summary>
[Authorize(Roles = Roles.Administrator)]
public sealed record GetItemMergePreviewQuery(Guid PrimaryItemId, IReadOnlyCollection<Guid> ItemIds)
    : IRequest<Result<ItemMergePreviewDto>>;
