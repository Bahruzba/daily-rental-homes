namespace DailyRentalHomes.Api.Contracts.Bookings;

public sealed class ChangeBookingStatusRequest
{
    public long NewStatusId { get; set; }
    public long? ChangedByUserId { get; set; }
    public string? Note { get; set; }
}
