using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationSplit;

public sealed class SaveInvitationSplitCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SaveInvitationSplitCommand, Result<InvitationSplitDto>>
{
    public async ValueTask<Result<InvitationSplitDto>> Handle(SaveInvitationSplitCommand request,
        CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);
        if (configuration is null)
        {
            configuration = new SharedClassConfiguration();
            dbContext.SharedClassConfigurations.Add(configuration);
        }

        configuration.StudentInvitationCount = request.StudentInvitationCount!.Value;
        configuration.InstructorInvitationCount = request.InstructorInvitationCount!.Value;
        configuration.OpeningPrayerInvitationCount = request.OpeningPrayerInvitationCount!.Value;
        configuration.ExperienceInvitationCount = request.ExperienceInvitationCount!.Value;
        configuration.ChangeInvitationCount = request.ChangeInvitationCount!.Value;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok(new InvitationSplitDto(
            configuration.StudentInvitationCount,
            configuration.InstructorInvitationCount,
            configuration.OpeningPrayerInvitationCount,
            configuration.ExperienceInvitationCount,
            configuration.ChangeInvitationCount));
    }
}
