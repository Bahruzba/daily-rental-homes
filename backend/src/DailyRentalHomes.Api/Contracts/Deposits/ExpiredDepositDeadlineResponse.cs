using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;

namespace DailyRentalHomes.Api.Contracts.Deposits;

public sealed record ExpiredDepositDeadlineResponse(
    long BookingId,
    long DepositId,
    long RentalHomeId,
    string RentalHomeTitle,
    long? CustomerId,
    string CustomerName,
    DateTime DeadlineAt,
    string DepositStatus,
    string BookingStatus)
{
    public static ExpiredDepositDeadlineResponse FromEntity(BookingDeposit deposit) => new(
        deposit.BookingId,
        deposit.Id,
        deposit.Booking?.RentalHomeId ?? 0,
        deposit.Booking?.RentalHome?.Title ?? string.Empty,
        deposit.Booking?.CustomerUserId,
        deposit.Booking?.CustomerFullName ?? string.Empty,
        deposit.DeadlineAt!.Value,
        DepositStatusCodes.FromStatus(deposit.Status),
        deposit.Booking?.Status?.Code ?? string.Empty);
}
