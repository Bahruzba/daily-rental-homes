using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class BookingCancellationRequest : BaseEntity
{
    public long BookingId { get; set; }
    public long RequestedByUserId { get; set; }
    public string? Reason { get; set; }
    public string StatusCode { get; set; } = "pending";

    public Booking? Booking { get; set; }
    public User? RequestedByUser { get; set; }
}
