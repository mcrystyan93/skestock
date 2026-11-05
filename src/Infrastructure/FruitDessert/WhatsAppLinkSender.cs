using FluentResults;
using skestock.Application.Features.FruitDessertPlans.Abstractions;
using skestock.Domain.Enums;

namespace skestock.Infrastructure.FruitDessert;

// No delivery confirmation: the user presses "Send" in WhatsApp, so the status stays Opened.
public class WhatsAppLinkSender : IMessageSender
{
    public Task<Result<MessageDispatch>> SendAsync(string recipientPhone, string body, CancellationToken cancellationToken)
    {
        var url = $"https://wa.me/{recipientPhone}?text={Uri.EscapeDataString(body)}";
        return Task.FromResult(Result.Ok(new MessageDispatch(url, FruitDessertMessageStatus.Opened)));
    }
}
