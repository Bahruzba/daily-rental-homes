using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class BookingDate : BaseEntity
{
    public long BookingId { get; set; }
    public DateOnly Date { get; set; }

    public Booking? Booking { get; set; }
}
