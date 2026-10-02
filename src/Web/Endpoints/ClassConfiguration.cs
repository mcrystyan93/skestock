using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using skestock.Application.Features.ClassConfiguration.Commands.DeleteDepartment;
using skestock.Application.Features.ClassConfiguration.Models;
using skestock.Domain.Constants;
using skestock.Application.Features.ClassConfiguration.Commands.SaveDepartment;
using skestock.Application.Features.ClassConfiguration.Commands.SaveInvitationCount;
using skestock.Application.Features.ClassConfiguration.Commands.SaveRoomConfiguration;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartments;
using skestock.Application.Features.ClassConfiguration.Queries.GetDepartment;
using skestock.Application.Features.ClassConfiguration.Queries.GetInvitationCount;
using skestock.Application.Features.ClassConfiguration.Queries.GetRoomConfiguration;

namespace skestock.Web.Endpoints;

public sealed class ClassConfiguration : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetDepartments, "departments").RequireAuthorization();
        groupBuilder.MapGet(GetDepartment, "departments/{id:guid}").RequireAuthorization();
        groupBuilder.MapPut(SaveDepartment, "departments")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        groupBuilder.MapGet(GetInvitations, "invitations").RequireAuthorization();
        groupBuilder.MapPut(SaveInvitations, "invitations")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        groupBuilder.MapGet(GetRooms, "rooms").RequireAuthorization();
        groupBuilder.MapPut(SaveRooms, "rooms").RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
        groupBuilder.MapDelete(DeleteDepartment, "departments/{id:guid}")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public static async Task<Results<Ok<IReadOnlyList<DepartmentTemplateDto>>, ProblemHttpResult>> GetDepartments(
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDepartmentsQuery(), cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<Ok<DepartmentTemplateDto>, ProblemHttpResult>> GetDepartment(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDepartmentQuery { Id = id }, cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<Ok<DepartmentTemplateDto>, ProblemHttpResult>> SaveDepartment(
        ISender sender, [FromBody] SaveDepartmentCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<Ok<InvitationCountDto>, ProblemHttpResult>> GetInvitations(
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvitationCountQuery(), cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<Ok<InvitationCountDto>, ProblemHttpResult>> SaveInvitations(
        ISender sender, [FromBody] SaveInvitationCountCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<Ok<RoomConfigurationDto>, ProblemHttpResult>> GetRooms(
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetRoomConfigurationQuery(), cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<Ok<RoomConfigurationDto>, ProblemHttpResult>> SaveRooms(
        ISender sender, [FromBody] SaveRoomConfigurationCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return result.ToOk();
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteDepartment(
        ISender sender, Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteDepartmentCommand { Id = id }, cancellationToken);
        return result.ToNoContent();
    }
}
