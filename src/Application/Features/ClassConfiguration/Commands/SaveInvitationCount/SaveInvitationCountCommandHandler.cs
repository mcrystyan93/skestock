using skestock.Application.Common.Interfaces;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Entities;
using skestock.Domain.Entities.SchoolClasses;

namespace skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;

public sealed class SaveInvitationCountCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SaveInvitationCountCommand, Result<InvitationCountDto>>
{
    public async ValueTask<Result<InvitationCountDto>> Handle(SaveInvitationCountCommand request, CancellationToken cancellationToken)
    {
        var configuration = await dbContext.SharedClassConfigurations
            .SingleOrDefaultAsync(item => item.Id == SharedClassConfiguration.SingletonId, cancellationToken);
        if (configuration is null)
        {
            configuration = new SharedClassConfiguration();
            dbContext.SharedClassConfigurations.Add(configuration);
        }
        configuration.InvitationCount = request.InvitationCount;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok(new InvitationCountDto(configuration.InvitationCount));
    }
}
