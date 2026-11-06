namespace skestock.Application.Features.FruitDessertPlans.Abstractions;

public interface IMessageSender
{
    /// <summary>Prepares the dispatch of <paramref name="body"/> to a normalized phone number.</summary>
    Task<Result<MessageDispatch>> SendAsync(string recipientPhone, string body, CancellationToken cancellationToken);
}
