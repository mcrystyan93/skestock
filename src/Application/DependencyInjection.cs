using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using skestock.Application.Common.Behaviours;

namespace skestock.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Services.AddMediator(options =>
        {
            // Handlers depend on scoped services (e.g. IApplicationDbContext), so the
            // mediator and its pipeline must be registered with a matching scoped lifetime.
            options.ServiceLifetime = ServiceLifetime.Scoped;

            // Order is significant - see AGENTS.md "Mediator pipeline" section.
            options.PipelineBehaviors =
            [
                typeof(LoggingBehaviour<,>),
                typeof(UnhandledExceptionBehaviour<,>),
                typeof(AuthorizationBehaviour<,>),
                typeof(ValidationBehaviour<,>),
                typeof(PerformanceBehaviour<,>),
                typeof(CachingBehavior<,>),
                typeof(CacheInvalidationBehavior<,>)
            ];
        });
    }
}
