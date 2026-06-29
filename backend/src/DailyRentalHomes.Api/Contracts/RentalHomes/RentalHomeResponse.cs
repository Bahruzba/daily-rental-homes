namespace DailyRentalHomes.Api.Contracts.RentalHomes;

public sealed record RentalHomeResponse(
    long Id,
    string Title,
    string City,
    string? District,
    decimal DailyPrice,
    int RoomCount,
    int GuestCount,
    bool IsPublished);
