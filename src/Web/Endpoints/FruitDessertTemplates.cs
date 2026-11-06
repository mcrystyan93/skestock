using Microsoft.AspNetCore.Http.HttpResults;
using skestock.Application.Features.FruitDessertTemplates.Commands.UpdateMessageTemplate;
using skestock.Application.Features.FruitDessertTemplates.Models;
using skestock.Application.Features.FruitDessertTemplates.Queries.GetMessageTemplates;
using skestock.Domain.Enums;

namespace skestock.Web.Endpoints;

public class FruitDessertTemplates : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMessageTemplates);
        groupBuilder.MapPut(UpdateMessageTemplate, "{type}");
    }

    [EndpointSummary("Get the fruit and dessert message templates")]
    [EndpointDescription("Returns the reminder and thanks templates (HTML body) and the placeholders usable in them.")]
    public static async Task<Results<Ok<MessageTemplatesDto>, ProblemHttpResult>> GetMessageTemplates(
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMessageTemplatesQuery(), cancellationToken);

        return result.ToOk();
    }

    [EndpointSummary("Update a fruit and dessert message template")]
    [EndpointDescription("Replaces the name and body of the active template of the given type. The body is sanitized to a small HTML whitelist.")]
    public static async Task<Results<NoContent, ProblemHttpResult>> UpdateMessageTemplate(
        ISender sender, string type, UpdateMessageTemplateRequest request,
        CancellationToken cancellationToken)
    {
        // Route enum binding is case-sensitive; the client sends camelCase.
        if (!Enum.TryParse<FruitDessertMessageType>(type, ignoreCase: true, out var messageType)
            || !Enum.IsDefined(messageType))
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid template type",
                detail: "type must be 'reminder' or 'thanks'.");

        var result = await sender.Send(new UpdateMessageTemplateCommand(messageType, request.Name, request.Body),
            cancellationToken);

        return result.ToNoContent();
    }
}
