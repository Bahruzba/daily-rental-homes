namespace DailyRentalHomes.Api.Contracts.RentalHomes;

public sealed record RentalHomeResponse(
    long Id,
    string Title,
    string Description,
    string City,
    string? District,
    decimal DailyPrice,
    int RoomCount,
    int GuestCount,
    bool IsPublished,
    string? MainImageUrl);

public sealed record RentalHomeDetailResponse(
    long Id,
    string Title,
    string Description,
    string City,
    string? District,
    string? Address,
    decimal DailyPrice,
    int RoomCount,
    int GuestCount,
    bool IsPublished,
    List<RentalHomeMediaResponse> MediaFiles,
    List<RentalHomeContactResponse> Contacts,
    List<RentalHomeUnavailableRangeResponse> UnavailableRanges);

public sealed record RentalHomeMediaResponse(string FileUrl, int SortOrder);

public sealed record RentalHomeContactResponse(string FullName, string Value, int ContactType);

public sealed record RentalHomeUnavailableRangeResponse(DateOnly StartDate, DateOnly EndDate);
