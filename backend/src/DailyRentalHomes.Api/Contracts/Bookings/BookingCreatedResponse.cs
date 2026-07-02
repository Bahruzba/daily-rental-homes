namespace DailyRentalHomes.Api.Contracts.Bookings;

public sealed record BookingCreatedResponse(
    long BookingId,
    long RentalHomeId,
    string StatusCode,
    string StatusName,
    decimal DailyPrice,
    decimal TotalAmount,
    List<DateOnly> Dates,
    string CustomerName,
    string Phone,
    DateTime CreatedAt);
