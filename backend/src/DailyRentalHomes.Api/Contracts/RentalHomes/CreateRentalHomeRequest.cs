namespace DailyRentalHomes.Api.Contracts.RentalHomes;

public sealed record CreateRentalHomeRequest(
    long BrokerUserId,
    string Title,
    string Description,
    string City,
    string? District,
    string? Address,
    decimal DailyPrice,
    int RoomCount,
    int GuestCount);
