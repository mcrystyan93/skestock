using System.Reflection;
using skestock.Application.Common.Exceptions;
using skestock.Application.Common.Interfaces;
using skestock.Application.Common.Security;

namespace skestock.Application.Common.Behaviours;

public class AuthorizationBehaviour<TRequest, TResponse>(
    IUser user,
    IIdentityService identityService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IMessage
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        var authorizeAttributes = request.GetType().GetCustomAttributes<AuthorizeAttribute>();

        IEnumerable<AuthorizeAttribute> attributes = authorizeAttributes.ToList();
        if (attributes.Any())
        {
            // Must be authenticated user
            if (user.Id == null)
            {
                throw new UnauthorizedAccessException();
            }

            // Role-based authorization
            var authorizeAttributesWithRoles = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Roles));

            IEnumerable<AuthorizeAttribute> attributesWithRoles = authorizeAttributesWithRoles.ToList();
            if (attributesWithRoles.Any())
            {
                var authorized = false;

                foreach (var roles in attributesWithRoles.Select(a => a.Roles.Split(',')))
                {
                    foreach (var role in roles)
                    {
                        var isInRole = user.Roles?.Any(x => role == x)??false;
                        if (isInRole)
                        {
                            authorized = true;
                            break;
                        }
                    }
                }

                // Must be a member of at least one role in roles
                if (!authorized)
                {
                    throw new ForbiddenAccessException();
                }
            }

            // Policy-based authorization
            var authorizeAttributesWithPolicies = attributes.Where(a => !string.IsNullOrWhiteSpace(a.Policy));
            IEnumerable<AuthorizeAttribute> attributesWithPolicies = authorizeAttributesWithPolicies.ToList();
            if (attributesWithPolicies.Any())
            {
                foreach (var policy in attributesWithPolicies.Select(a => a.Policy))
                {
                    if(user.Id == null)
                        throw new UnauthorizedAccessException();
                    
                    var authorized = await identityService.AuthorizeAsync(user.Id.Value, policy);

                    if (!authorized)
                    {
                        throw new ForbiddenAccessException();
                    }
                }
            }
        }

        // User is authorized / authorization not required
        return await next(request, cancellationToken);
    }
}
