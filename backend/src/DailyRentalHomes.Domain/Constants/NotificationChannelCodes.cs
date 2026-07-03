using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Constants;

public static class NotificationChannelCodes
{
    public const string WhatsApp = "whatsapp";
    public const string Sms = "sms";
    public const string InApp = "in_app";

    public static string From(MessageChannel channel) => channel switch
    {
        MessageChannel.WhatsApp => WhatsApp,
        MessageChannel.Sms => Sms,
        MessageChannel.InApp => InApp,
        _ => throw new ArgumentOutOfRangeException(nameof(channel))
    };
}
