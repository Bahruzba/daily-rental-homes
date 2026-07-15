namespace DailyRentalHomes.Api.Options;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "NotificationDelivery";
    public const string FakeProvider = "Fake";
    public const string MetaWhatsAppProvider = "MetaWhatsApp";

    public string Provider { get; set; } = FakeProvider;
    public MetaWhatsAppOptions MetaWhatsApp { get; set; } = new();
}

public sealed class MetaWhatsAppOptions
{
    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string WebhookVerifyToken { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}
