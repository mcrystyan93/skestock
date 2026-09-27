using skestock.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace skestock.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse>(
    ILogger<TRequest> logger,
    IUser user)
    : MessagePreProcessor<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    protected override ValueTask Handle(TRequest request, CancellationToken cancellationToken)
    {
        logger.HandlingRequest(typeof(TRequest).Name, user.Id);

        return ValueTask.CompletedTask;
    }
}
