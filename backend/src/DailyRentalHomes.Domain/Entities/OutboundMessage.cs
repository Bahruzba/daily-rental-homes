using DailyRentalHomes.Domain.Common;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Entities;

public sealed class OutboundMessage : BaseEntity
{
    public long? RecipientUserId { get; set; }
    public string? RecipientName { get; set; }
    public MessageChannel Channel { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public string TypeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime? SentAt { get; set; }
    public long? BookingId { get; set; }
    public long? BookingDepositId { get; set; }

    public User? RecipientUser { get; set; }
    public Booking? Booking { get; set; }
    public BookingDeposit? BookingDeposit { get; set; }
}
