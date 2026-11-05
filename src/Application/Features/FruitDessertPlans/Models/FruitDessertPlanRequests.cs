using skestock.Domain.Enums;

namespace skestock.Application.Features.FruitDessertPlans.Models;

public static class FruitDessertPlanRequests
{
    public class SetCongregationRequest
    {
        public Guid? CongregationId { get; init; }
    }

    public class SetEmailSentRequest
    {
        public bool EmailSent { get; init; }
    }

    public class SendMessageRequest
    {
        public FruitDessertMessageType Type { get; init; }
    }
}
