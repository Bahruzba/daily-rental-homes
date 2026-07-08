namespace DailyRentalHomes.Api.Contracts.Notifications;

public sealed class ProcessPendingNotificationsRequest
{
    public int? BatchSize { get; set; }
}

public sealed record ProcessPendingNotificationsResponse(int Processed, int Sent, int Failed);
