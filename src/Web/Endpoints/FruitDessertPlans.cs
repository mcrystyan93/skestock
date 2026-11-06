using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.FruitDessertPlans.Commands.SendFruitDessertMessage;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertCongregation;
using skestock.Application.Features.FruitDessertPlans.Commands.SetFruitDessertEmailSent;
using skestock.Application.Features.FruitDessertPlans.Models;
using skestock.Application.Features.FruitDessertPlans.Queries.GetFruitDessertPlan;

namespace skestock.Web.Endpoints;

public class FruitDessertPlans : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetFruitDessertPlan, "{classId:guid}");
        groupBuilder.MapPut(SetFruitDessertCongregation, "{classId:guid}/entries/{entryId:guid}/congregation");
        groupBuilder.MapPut(SetFruitDessertEmailSent, "{classId:guid}/entries/{entryId:guid}/email-sent");
        groupBuilder.MapPost(SendFruitDessertMessage, "{classId:guid}/entries/{entryId:guid}/messages");
    }

    [EndpointSummary("Get the fruit and dessert plan of a class")]
    [EndpointDescription("Creates the missing working days of the class interval, then lists the plan ordered by date.")]
    public static async Task<Results<Ok<IReadOnlyList<FruitDessertPlanEntryDto>>, ProblemHttpResult>> GetFruitDessertPlan(
        ISender sender, Guid classId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFruitDessertPlanQuery { ClassId = classId }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Set the congregation of a plan day")]
    [EndpointDescription("Assigns the congregation that brings fruit and dessert, or clears it when the id is null.")]
    public static async Task<Results<Ok<FruitDessertPlanEntryDto>, ProblemHttpResult>> SetFruitDessertCongregation(
        ISender sender, Guid classId, Guid entryId, FruitDessertPlanRequests.SetCongregationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetFruitDessertCongregationCommand
        {
            ClassId = classId, EntryId = entryId, CongregationId = request.CongregationId
        }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Set the email-sent flag of a plan day")]
    [EndpointDescription("Marks whether the email to the congregation was sent. This is a manual flag; no email is sent.")]
    public static async Task<Results<Ok<FruitDessertPlanEntryDto>, ProblemHttpResult>> SetFruitDessertEmailSent(
        ISender sender, Guid classId, Guid entryId, FruitDessertPlanRequests.SetEmailSentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetFruitDessertEmailSentCommand
        {
            ClassId = classId, EntryId = entryId, EmailSent = request.EmailSent
        }, cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Send a reminder or thanks message for a plan day")]
    [EndpointDescription("Renders the active template, records the dispatch in the history and returns the WhatsApp URL to open.")]
    public static async Task<Results<Ok<FruitDessertMessageDispatchDto>, ProblemHttpResult>> SendFruitDessertMessage(
        ISender sender, Guid classId, Guid entryId, FruitDessertPlanRequests.SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendFruitDessertMessageCommand
        {
            ClassId = classId, EntryId = entryId, Type = request.Type
        }, cancellationToken);

        return result.ToOk();
    }
}
