namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed class NewDepositRequest
{
    public long BookingId { get; set; }
    public decimal Amount { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public long? PaymentCardId { get; set; }
    public string? Note { get; set; }
}
