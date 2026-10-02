using skestock.Application.Common.Security;
using skestock.Application.Features.ClassConfiguration.Models;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetSharedClassConfiguration;

[Authorize]
public sealed class GetSharedClassConfigurationQuery : IRequest<Result<SharedClassConfigurationDto>>;
