namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed class SendDepositReminderInput
{
    public string To { get; set; } = string.Empty;
    public string? CustomText { get; set; }
}
