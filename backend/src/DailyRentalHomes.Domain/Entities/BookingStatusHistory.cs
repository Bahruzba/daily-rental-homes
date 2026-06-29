using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class BookingStatusHistory : BaseEntity
{
    public long BookingId { get; set; }
    public long? OldStatusId { get; set; }
    public long NewStatusId { get; set; }
    public long? ChangedByUserId { get; set; }
    public string? Note { get; set; }

    public Booking? Booking { get; set; }
    public BookingStatus? OldStatus { get; set; }
    public BookingStatus? NewStatus { get; set; }
    public User? ChangedByUser { get; set; }
}
