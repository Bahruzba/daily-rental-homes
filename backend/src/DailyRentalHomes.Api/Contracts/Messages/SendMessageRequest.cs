using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Api.Contracts.Messages;

public sealed class SendMessageRequest
{
    public MessageChannel Channel { get; set; } = MessageChannel.WhatsApp;
    public string To { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public long? BookingId { get; set; }
    public long? BookingDepositId { get; set; }
}
