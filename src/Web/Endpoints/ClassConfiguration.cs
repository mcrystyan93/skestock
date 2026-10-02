using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using skestock.Application.Features.ClassConfiguration.Commands.SaveSharedClassConfiguration;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Application.Features.ClassConfiguration.Queries.GetSharedClassConfiguration;
using skestock.Domain.Constants;

namespace skestock.Web.Endpoints;

public sealed class ClassConfiguration : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(Get, "").RequireAuthorization();
        groupBuilder.MapPut(Save, "").RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public static async Task<Results<Ok<SharedClassConfigurationDto>, ProblemHttpResult>> Get(
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSharedClassConfigurationQuery(), cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<Ok<SharedClassConfigurationDto>, ProblemHttpResult>> Save(
        ISender sender, [FromBody] SaveSharedClassConfigurationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk();
    }
}
