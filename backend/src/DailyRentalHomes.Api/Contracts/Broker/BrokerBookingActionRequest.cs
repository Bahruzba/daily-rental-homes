namespace DailyRentalHomes.Api.Contracts.Broker;

public sealed class BrokerBookingActionRequest
{
    public string? Note { get; set; }
}

public sealed class BrokerCancellationDecisionRequest
{
    public string? Note { get; set; }
}
