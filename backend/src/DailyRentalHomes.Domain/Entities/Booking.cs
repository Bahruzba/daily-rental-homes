using DailyRentalHomes.Domain.Common;

namespace DailyRentalHomes.Domain.Entities;

public sealed class Booking : BaseEntity
{
    public long RentalHomeId { get; set; }
    public long? CustomerUserId { get; set; }
    public string CustomerFullName { get; set; } = string.Empty;
    public string CustomerPhoneNumber { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public decimal DailyPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public long StatusId { get; set; }
    public string? CustomerNote { get; set; }
    public string Source { get; set; } = "web";

    public RentalHome? RentalHome { get; set; }
    public User? CustomerUser { get; set; }
    public BookingStatus? Status { get; set; }
    public BookingDeposit? Deposit { get; set; }
    public ICollection<BookingDate> Dates { get; set; } = new List<BookingDate>();
    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = new List<BookingStatusHistory>();
}
