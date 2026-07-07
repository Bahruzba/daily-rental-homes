namespace DailyRentalHomes.Api.Contracts.Broker;

public sealed record BrokerRentalHomeDetailResponse(
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
    IReadOnlyList<BrokerRentalHomeMediaResponse> Media,
    int BookingCount,
    int UpcomingBookingCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record BrokerRentalHomeMediaResponse(
    long Id,
    string Url,
    string Type,
    bool IsMain,
    int SortOrder,
    string? ContentType,
    long? SizeBytes);

public sealed record BrokerRentalHomeSaveRequest(
    string Title,
    string Description,
    string City,
    string? District,
    string? Address,
    decimal DailyPrice,
    int RoomCount,
    int GuestCount,
    bool? IsPublished);

public sealed record BrokerRentalHomeSaveResponse(long Id);

public sealed record BrokerRentalHomeMediaUploadResponse(
    long Id,
    string Url,
    string Type,
    bool IsMain,
    int SortOrder,
    string? ContentType,
    long? SizeBytes);
