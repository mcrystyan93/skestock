using System.Reflection;

namespace skestock.Web.Infrastructure;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Discovers all <see cref="IEndpointGroup"/> implementations in <paramref name="assembly"/>
    /// and registers each as a route group with a matching OpenAPI tag. The route prefix defaults
    /// to <c>/api/{ClassName}</c> but can be overridden via <see cref="IEndpointGroup.RoutePrefix"/>.
    /// </summary>
    public static WebApplication MapEndpoints(this WebApplication app, Assembly assembly)
    {
        var endpointGroupTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                     && t.IsAssignableTo(typeof(IEndpointGroup)));

        foreach (var type in endpointGroupTypes)
        {
            var groupName = type.Name;
            var routePrefix = type.GetProperty(nameof(IEndpointGroup.RoutePrefix))
                ?.GetValue(null) as string ?? $"/api/{groupName}";
            var group = app.MapGroup(routePrefix).WithTags(groupName);
            var requiresAuthorization = type.GetProperty(nameof(IEndpointGroup.RequiresAuthorization))
                ?.GetValue(null) as bool? ?? true;
            if (requiresAuthorization)
            {
                group.RequireAuthorization();
            }

            type.GetMethod(nameof(IEndpointGroup.Map))!.Invoke(null, [group]);
        }

        return app;
    }

    /// <summary>
    /// Answers 404 for the Identity self-registration endpoint. <c>MapIdentityApi</c> maps it as part
    /// of one route group and offers no way to omit it, so it is short-circuited before routing and
    /// model binding. Accounts are created by administrators only.
    /// </summary>
    public static IApplicationBuilder UseIdentityRegistrationDisabled(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.Equals("/api/Users/register", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            await next();
        });
}
