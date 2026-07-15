namespace DailyRentalHomes.Api.Options;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "NotificationDelivery";
    public const string FakeProvider = "Fake";

    public string Provider { get; set; } = FakeProvider;
}
