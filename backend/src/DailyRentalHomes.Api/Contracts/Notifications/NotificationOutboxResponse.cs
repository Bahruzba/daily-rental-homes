namespace DailyRentalHomes.Api.Contracts.Notifications;

public sealed record NotificationOutboxResponse(
    long Id,
    string Type,
    string Channel,
    string Status,
    long? RecipientUserId,
    string? RecipientName,
    string RecipientPhone,
    string Title,
    string Message,
    DateTime? ScheduledAt,
    DateTime? SentAt,
    string? ProviderMessageId,
    string? ProviderDeliveryStatus,
    DateTime? ProviderStatusUpdatedAt,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    string? ErrorMessage,
    long? RelatedBookingId,
    long? RelatedDepositId,
    DateTime CreatedAt);
