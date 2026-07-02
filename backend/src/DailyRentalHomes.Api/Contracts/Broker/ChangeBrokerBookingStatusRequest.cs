namespace DailyRentalHomes.Api.Contracts.Broker;

public sealed class ChangeBrokerBookingStatusRequest
{
    public string StatusCode { get; set; } = string.Empty;
    public string? Note { get; set; }
}
