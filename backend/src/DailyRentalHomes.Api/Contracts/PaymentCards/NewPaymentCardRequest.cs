namespace DailyRentalHomes.Api.Contracts.PaymentCards;

public sealed class NewPaymentCardRequest
{
    public long BrokerUserId { get; set; }
    public string CardHolderName { get; set; } = string.Empty;
    public string PanMasked { get; set; } = string.Empty;
}
