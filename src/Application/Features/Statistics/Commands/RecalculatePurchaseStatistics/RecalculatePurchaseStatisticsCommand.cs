using skestock.Application.Common.Security;
using skestock.Domain.Constants;

namespace skestock.Application.Features.Statistics.Commands.RecalculatePurchaseStatistics;

// On-demand entry point for the daily job's purchase statistics work. The Worker sends the inner
// MaterializePurchaseStatisticsCommand without a user, so the administrator check lives here.
[Authorize(Roles = Roles.Administrator)]
public sealed class RecalculatePurchaseStatisticsCommand : IRequest<Result<int>>;
