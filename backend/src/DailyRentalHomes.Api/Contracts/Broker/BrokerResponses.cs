namespace DailyRentalHomes.Api.Contracts.Broker;

public sealed record BrokerSummaryResponse(
    int TotalHomes,
    int ActiveHomes,
    int TotalBookings,
    int PendingBookings,
    int PendingDepositBookings,
    int UpcomingBookings,
    decimal TotalExpectedAmount);

public sealed record BrokerRentalHomeResponse(
    long Id,
    string Title,
    string City,
    string? District,
    string? Address,
    decimal DailyPrice,
    int GuestCount,
    bool IsPublished,
    string? MainImageUrl,
    int BookingCount);

public sealed record BrokerBookingListItemResponse(
    long BookingId,
    long RentalHomeId,
    string RentalHomeTitle,
    string CustomerName,
    string CustomerPhone,
    string StatusCode,
    string StatusName,
    decimal TotalAmount,
    int DatesCount,
    DateOnly? FirstDate,
    DateOnly? LastDate,
    DateTime CreatedAt,
    string? Note,
    bool IsDepositPending);

public sealed record BrokerBookingDetailResponse(
    long BookingId,
    BrokerBookingHomeResponse RentalHome,
    BrokerBookingCustomerResponse Customer,
    BrokerBookingStatusResponse Status,
    decimal DailyPrice,
    decimal TotalAmount,
    IReadOnlyList<DateOnly> Dates,
    int Guests,
    string? Note,
    DateTime CreatedAt,
    IReadOnlyList<BrokerBookingStatusHistoryResponse> StatusHistory,
    BrokerBookingDepositResponse? Deposit);

public sealed record BrokerBookingHomeResponse(long Id, string Title, string City, string? District);
public sealed record BrokerBookingCustomerResponse(string FullName, string Phone);
public sealed record BrokerBookingStatusResponse(string Code, string Name);
public sealed record BrokerBookingStatusHistoryResponse(
    string? OldStatusCode,
    string NewStatusCode,
    string? Note,
    DateTime ChangedAt);
public sealed record BrokerBookingDepositResponse(decimal Amount, string Status, DateTime? DeadlineAt, DateTime? PaidAt);

public sealed record BrokerBookingStatusChangeResponse(long BookingId, string StatusCode, string StatusName);
