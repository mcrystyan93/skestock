namespace skestock.Application.Features.Items.Commands.MergeItems;

// Stub so the source-generated Mediator builds; replaced in T6.
public class MergeItemsCommandHandler : IRequestHandler<MergeItemsCommand, Result>
{
    public ValueTask<Result> Handle(MergeItemsCommand request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Result.Ok());
}
