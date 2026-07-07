using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Constants;

public static class NotificationStatusCodes
{
    public const string Pending = "pending";
    public const string Sent = "sent";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string Skipped = "skipped";

    public static string From(MessageStatus status) => status switch
    {
        MessageStatus.Pending => Pending,
        MessageStatus.Sent => Sent,
        MessageStatus.Failed => Failed,
        MessageStatus.Cancelled => Cancelled,
        MessageStatus.Skipped => Skipped,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
