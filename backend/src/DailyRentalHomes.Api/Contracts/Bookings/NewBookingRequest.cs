namespace DailyRentalHomes.Api.Contracts.Bookings;

public sealed class NewBookingRequest
{
    public long RentalHomeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Guests { get; set; }
    public List<DateOnly> Dates { get; set; } = new();
    public string? Note { get; set; }
}
