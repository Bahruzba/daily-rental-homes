namespace DailyRentalHomes.Api.Contracts.Account;

public sealed class CreateBookingCancellationRequest
{
    public string? Reason { get; set; }
}

public sealed record BookingCancellationRequestResponse(
    long Id,
    long BookingId,
    string StatusCode,
    string? Reason,
    DateTime CreatedAt);
