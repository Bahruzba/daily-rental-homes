using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class BookingExpense : BaseEntity
{
    public long BookingId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }

    public Booking? Booking { get; set; }
}
