using skestock.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace skestock.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse>(
    ILogger<TRequest> logger,
    IUser user,
    IIdentityService identityService)
    : MessagePreProcessor<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    private readonly ILogger _logger = logger;

    protected override async ValueTask Handle(TRequest request, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = user.Id;
        string? userName = string.Empty;

        if (userId!=null)
        {
            userName = await identityService.GetUserNameAsync(userId.Value);
        }

        _logger.LogInformation("skestock Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userId, userName, request);
    }
}
