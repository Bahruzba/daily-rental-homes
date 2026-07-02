namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed class RequestBookingDepositInput
{
    public decimal Amount { get; set; }
    public DateTime DeadlineAt { get; set; }
    public string? CardHolderName { get; set; }
    public string CardPanMasked { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? Note { get; set; }
}
