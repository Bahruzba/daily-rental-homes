using DailyRentalHomes.Api.Contracts.Deposits;

namespace DailyRentalHomes.Api.Contracts.Account;

public sealed record AccountBookingListItemResponse(
    long BookingId,
    string RentalHomeTitle,
    string City,
    string? District,
    string StatusCode,
    string StatusName,
    decimal TotalAmount,
    IReadOnlyList<DateOnly> Dates,
    DepositResponse? Deposit,
    DateTime CreatedAt);

public sealed record AccountBookingDetailResponse(
    long BookingId,
    long RentalHomeId,
    string RentalHomeTitle,
    string City,
    string? District,
    string StatusCode,
    string StatusName,
    decimal DailyPrice,
    decimal TotalAmount,
    int Guests,
    IReadOnlyList<DateOnly> Dates,
    string? Note,
    DepositResponse? Deposit,
    DateTime CreatedAt);
