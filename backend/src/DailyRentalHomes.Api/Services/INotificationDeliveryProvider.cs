using DailyRentalHomes.Domain.Entities;

namespace DailyRentalHomes.Api.Services;

public interface INotificationDeliveryProvider
{
    Task<NotificationDeliveryResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken);
}

public sealed record NotificationDeliveryResult(bool Success, string? ProviderMessageId, string? ErrorMessage, bool IsRetryable)
{
    public static NotificationDeliveryResult Sent(string providerMessageId) => new(true, providerMessageId, null, false);
    public static NotificationDeliveryResult Failed(string errorMessage) => new(false, null, errorMessage, false);
    public static NotificationDeliveryResult RetryableFailed(string errorMessage) => new(false, null, errorMessage, true);
}
