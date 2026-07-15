namespace DailyRentalHomes.Api.Options;

public sealed class DepositReminderOptions
{
    public const string SectionName = "DepositReminderOptions";

    public int ReminderBeforeHours { get; set; } = 24;
    public int ProcessingIntervalMinutes { get; set; } = 15;
}
