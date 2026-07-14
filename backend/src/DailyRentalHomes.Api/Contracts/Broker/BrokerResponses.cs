using DailyRentalHomes.Api.Contracts.Deposits;

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
    bool IsDepositPending,
    bool HasPendingCancellationRequest);

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
    DepositResponse? Deposit,
    BrokerCancellationRequestResponse? CancellationRequest);

public sealed record BrokerBookingHomeResponse(long Id, string Title, string City, string? District);
public sealed record BrokerBookingCustomerResponse(string FullName, string Phone);
public sealed record BrokerBookingStatusResponse(string Code, string Name);
public sealed record BrokerCancellationRequestResponse(
    long Id,
    long BookingId,
    string StatusCode,
    string? Reason,
    string? DecisionNote,
    DateTime CreatedAt,
    DateTime? DecidedAt);
public sealed record BrokerBookingStatusHistoryResponse(
    string? OldStatusCode,
    string NewStatusCode,
    string? Note,
    DateTime ChangedAt);
public sealed record BrokerBookingStatusChangeResponse(long BookingId, string StatusCode, string StatusName);

public sealed record BrokerCalendarEventResponse(
    long? BookingId,
    long RentalHomeId,
    string RentalHomeTitle,
    DateOnly StartDate,
    DateOnly EndDate,
    string? BookingStatus,
    string? CustomerName,
    string EventType);
