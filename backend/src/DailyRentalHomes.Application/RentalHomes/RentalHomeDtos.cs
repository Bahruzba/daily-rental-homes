namespace DailyRentalHomes.Application.RentalHomes;

public sealed record CreateRentalHomeDto(
    long BrokerUserId,
    string Title,
    string Description,
    string City,
    string? District,
    string? Address,
    decimal DailyPrice,
    int RoomCount,
    int GuestCount);

public sealed record RentalHomeListItemDto(
    long Id,
    string Title,
    string City,
    string? District,
    decimal DailyPrice,
    int RoomCount,
    int GuestCount,
    bool IsPublished);
