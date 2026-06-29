using DailyRentalHomes.Domain.Common;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Entities;

public sealed class OutboundMessage : BaseEntity
{
    public MessageChannel Channel { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Pending;
    public string To { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public long? BookingId { get; set; }
    public long? BookingDepositId { get; set; }

    public Booking? Booking { get; set; }
    public BookingDeposit? BookingDeposit { get; set; }
}
