using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetInvitationCount;

public sealed class GetInvitationCountQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetInvitationCountQuery, Result<InvitationCountDto>>
{
    public async ValueTask<Result<InvitationCountDto>> Handle(GetInvitationCountQuery request, CancellationToken cancellationToken)
    {
        var count = await dbContext.SharedClassConfigurations.AsNoTracking()
            .Where(configuration => configuration.Id == SharedClassConfiguration.SingletonId)
            .Select(configuration => (int?)configuration.InvitationCount)
            .SingleOrDefaultAsync(cancellationToken);
        return Result.Ok(new InvitationCountDto(count ?? 0));
    }
}
