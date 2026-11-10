using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.ClassConfiguration.Queries.GetInvitationSplit;

public sealed class GetInvitationSplitQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetInvitationSplitQuery, Result<InvitationSplitDto>>
{
    public async ValueTask<Result<InvitationSplitDto>> Handle(GetInvitationSplitQuery request,
        CancellationToken cancellationToken)
    {
        var split = await dbContext.SharedClassConfigurations.AsNoTracking()
            .Where(item => item.Id == SharedClassConfiguration.SingletonId)
            .Select(item => new InvitationSplitDto(
                item.StudentInvitationCount,
                item.InstructorInvitationCount,
                item.OpeningPrayerInvitationCount,
                item.ExperienceInvitationCount,
                item.ChangeInvitationCount))
            .SingleOrDefaultAsync(cancellationToken);

        return Result.Ok(split ?? new InvitationSplitDto(0, 0, 0, 0, 0));
    }
}
