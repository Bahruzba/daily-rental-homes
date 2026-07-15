using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Enums;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[Route("api/admin/deposit-deadlines")]
public sealed class AdminDepositDeadlinesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminDepositDeadlinesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("expired")]
    public async Task<IActionResult> GetExpired(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var deposits = await _db.BookingDeposits
            .AsNoTracking()
            .Include(deposit => deposit.Booking)!.ThenInclude(booking => booking!.Status)
            .Include(deposit => deposit.Booking)!.ThenInclude(booking => booking!.RentalHome)
            .Where(deposit =>
                !deposit.IsDeleted &&
                deposit.DeadlineAt.HasValue &&
                deposit.DeadlineAt.Value < now &&
                deposit.Status != BookingDepositStatus.Paid &&
                deposit.Booking != null &&
                !deposit.Booking.IsDeleted &&
                deposit.Booking.Status != null &&
                deposit.Booking.Status.Code != BookingStatusCodes.Cancelled &&
                deposit.Booking.Status.Code != BookingStatusCodes.Completed &&
                deposit.Booking.Status.Code != BookingStatusCodes.Rejected)
            .OrderBy(deposit => deposit.DeadlineAt)
            .ThenBy(deposit => deposit.Id)
            .ToListAsync(cancellationToken);
        var items = deposits.Select(ExpiredDepositDeadlineResponse.FromEntity).ToList();

        return Ok(ApiResponse<IReadOnlyList<ExpiredDepositDeadlineResponse>>.Ok(items));
    }
}
