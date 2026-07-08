using DailyRentalHomes.Domain.Entities;

namespace DailyRentalHomes.Api.Services;

public sealed class FakeNotificationDeliveryProvider : INotificationDeliveryProvider
{
    public const string FailureMarker = "FAIL_FAKE_PROVIDER";

    public Task<NotificationDeliveryResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        if (ContainsFailureMarker(message.Title) ||
            ContainsFailureMarker(message.Text) ||
            ContainsFailureMarker(message.To) ||
            ContainsFailureMarker(message.RecipientName))
        {
            return Task.FromResult(NotificationDeliveryResult.Failed("Fake provider failure marker was found."));
        }

        return Task.FromResult(NotificationDeliveryResult.Sent($"fake-{message.Id}"));
    }

    private static bool ContainsFailureMarker(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(FailureMarker, StringComparison.OrdinalIgnoreCase);
}
