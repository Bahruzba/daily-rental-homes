using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
[Route("api/broker/reports")]
public sealed class BrokerReportsController : ControllerBase
{
    private static readonly HashSet<string> RevenueStatusCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        BookingStatusCodes.Pending,
        BookingStatusCodes.WaitingDeposit,
        BookingStatusCodes.Confirmed,
        BookingStatusCodes.Paid,
        BookingStatusCodes.Completed
    };

    private readonly AppDbContext _db;

    public BrokerReportsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue != to.HasValue)
        {
            return BadRequest(ApiResponse<object>.Fail("Both from and to must be provided together."));
        }

        if (from > to)
        {
            return BadRequest(ApiResponse<object>.Fail("From date must be before or equal to to date."));
        }

        var bookingsQuery = _db.Bookings
            .AsNoTracking()
            .Where(booking => booking.RentalHome != null);

        if (!User.IsAdmin())
        {
            var userId = User.GetUserId();
            bookingsQuery = bookingsQuery.Where(booking => booking.RentalHome!.BrokerUserId == userId);
        }

        if (from.HasValue && to.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking =>
                booking.Dates.Any(date => date.Date >= from.Value && date.Date <= to.Value));
        }

        var bookings = await bookingsQuery
            .Select(booking => new ReportBooking(
                booking.Id,
                booking.TotalAmount,
                booking.Status == null ? string.Empty : booking.Status.Code))
            .ToListAsync(cancellationToken);

        var bookingIds = bookings.Select(booking => booking.Id).ToList();

        List<ReportExpense> expenses = [];
        if (bookingIds.Count > 0)
        {
            expenses = await _db.BookingExpenses
                .AsNoTracking()
                .Where(expense => bookingIds.Contains(expense.BookingId))
                .Select(expense => new ReportExpense(expense.TypeCode, expense.Amount))
                .ToListAsync(cancellationToken);
        }

        var revenueBookings = bookings
            .Where(booking => RevenueStatusCodes.Contains(booking.StatusCode))
            .ToList();

        var totalBookingAmount = revenueBookings.Sum(booking => booking.TotalAmount);
        var totalExpenses = expenses.Sum(expense => expense.Amount);
        var totalCleaningCost = expenses
            .Where(expense => string.Equals(expense.TypeCode, "cleaning", StringComparison.OrdinalIgnoreCase))
            .Sum(expense => expense.Amount);
        var totalOwnerPayout = expenses
            .Where(expense => string.Equals(expense.TypeCode, "owner_payout", StringComparison.OrdinalIgnoreCase))
            .Sum(expense => expense.Amount);
        var totalOtherExpenses = expenses
            .Where(expense =>
                !string.Equals(expense.TypeCode, "cleaning", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(expense.TypeCode, "owner_payout", StringComparison.OrdinalIgnoreCase))
            .Sum(expense => expense.Amount);

        var response = new BrokerReportSummaryResponse(
            bookings.Count,
            revenueBookings.Count,
            totalBookingAmount,
            totalExpenses,
            totalBookingAmount - totalExpenses,
            totalCleaningCost,
            totalOwnerPayout,
            totalOtherExpenses,
            from,
            to);

        return Ok(ApiResponse<BrokerReportSummaryResponse>.Ok(response));
    }

    private sealed record ReportBooking(long Id, decimal TotalAmount, string StatusCode);

    private sealed record ReportExpense(string TypeCode, decimal Amount);
}
