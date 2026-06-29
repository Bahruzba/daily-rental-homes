using DailyRentalHomes.Domain.Common;
using DailyRentalHomes.Domain.Enums;

namespace DailyRentalHomes.Domain.Entities;

public sealed class BookingDeposit : BaseEntity
{
    public long BookingId { get; set; }
    public decimal Amount { get; set; }
    public BookingDepositStatus Status { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public long? PaymentCardId { get; set; }
    public string? Note { get; set; }

    public Booking? Booking { get; set; }
    public PaymentCard? PaymentCard { get; set; }
    public ICollection<MediaFile> ReceiptFiles { get; set; } = new List<MediaFile>();
}
