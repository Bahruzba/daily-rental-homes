namespace DailyRentalHomes.Api.Options;

public sealed class NotificationWorkerOptions
{
    public const string SectionName = "Notifications";

    public bool WorkerEnabled { get; set; }
    public int PollSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 20;
}
