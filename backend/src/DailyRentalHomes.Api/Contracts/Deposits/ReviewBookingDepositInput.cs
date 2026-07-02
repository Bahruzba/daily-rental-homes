namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed class ReviewBookingDepositInput
{
    public string? Note { get; set; }
    public bool AllowReupload { get; set; } = true;
}
