using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Broker;
using DailyRentalHomes.Api.Contracts.Deposits;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
[Route("api/broker")]
public sealed class BrokerController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [BookingStatusCodes.Pending] = [BookingStatusCodes.Cancelled],
            [BookingStatusCodes.WaitingDeposit] = [BookingStatusCodes.Cancelled]
        };

    private readonly AppDbContext _db;

    public BrokerController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var homes = ScopeHomes(_db.RentalHomes.AsNoTracking());
        var bookings = ScopeBookings(_db.Bookings.AsNoTracking());
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = new BrokerSummaryResponse(
            await homes.CountAsync(cancellationToken),
            await homes.CountAsync(home => home.IsPublished, cancellationToken),
            await bookings.CountAsync(cancellationToken),
            await bookings.CountAsync(booking => booking.Status!.Code == BookingStatusCodes.Pending, cancellationToken),
            await bookings.CountAsync(booking => booking.Status!.Code == BookingStatusCodes.WaitingDeposit, cancellationToken),
            await bookings.CountAsync(
                booking => booking.Status!.Code != BookingStatusCodes.Cancelled &&
                           booking.Status.Code != BookingStatusCodes.Rejected &&
                           booking.Dates.Any(date => date.Date >= today),
                cancellationToken),
            await bookings
                .Where(booking => booking.Status!.Code != BookingStatusCodes.Cancelled && booking.Status.Code != BookingStatusCodes.Rejected)
                .SumAsync(booking => booking.TotalAmount, cancellationToken));

        return Ok(ApiResponse<BrokerSummaryResponse>.Ok(response));
    }

    [HttpGet("rental-homes")]
    public async Task<IActionResult> GetRentalHomes(CancellationToken cancellationToken)
    {
        var items = await ScopeHomes(_db.RentalHomes.AsNoTracking())
            .OrderByDescending(home => home.Id)
            .Select(home => new BrokerRentalHomeResponse(
                home.Id,
                home.Title,
                home.City,
                home.District,
                home.Address,
                home.DailyPrice,
                home.GuestCount,
                home.IsPublished,
                home.MediaFiles.OrderBy(media => media.SortOrder).Select(media => media.FileUrl).FirstOrDefault(),
                _db.Bookings.Count(booking => booking.RentalHomeId == home.Id)))
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<BrokerRentalHomeResponse>>.Ok(items));
    }

    [HttpGet("bookings")]
    public async Task<IActionResult> GetBookings(
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
        {
            return BadRequest(ApiResponse<object>.Fail("The from date must not be after the to date."));
        }

        var query = ScopeBookings(_db.Bookings.AsNoTracking())
            .Include(booking => booking.RentalHome)
            .Include(booking => booking.Status)
            .Include(booking => booking.Dates)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusCode = status.Trim().ToLowerInvariant();
            query = query.Where(booking => booking.Status!.Code == statusCode);
        }

        if (from.HasValue)
        {
            query = query.Where(booking => booking.Dates.Any(date => date.Date >= from.Value));
        }

        if (to.HasValue)
        {
            query = query.Where(booking => booking.Dates.Any(date => date.Date <= to.Value));
        }

        var bookings = await query.OrderByDescending(booking => booking.CreatedAt).ThenByDescending(booking => booking.Id).ToListAsync(cancellationToken);
        var items = bookings.Select(ToListItem).ToList();

        return Ok(ApiResponse<IReadOnlyList<BrokerBookingListItemResponse>>.Ok(items));
    }

    [HttpGet("bookings/{id:long}")]
    public async Task<IActionResult> GetBookingById(long id, CancellationToken cancellationToken)
    {
        var booking = await ScopeBookings(_db.Bookings.AsNoTracking())
            .AsSplitQuery()
            .Include(item => item.RentalHome)
            .Include(item => item.Status)
            .Include(item => item.Dates)
            .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.PaymentCard)
            .Include(item => item.Deposit)!.ThenInclude(deposit => deposit!.ReceiptFiles)
            .Include(item => item.StatusHistory).ThenInclude(history => history.OldStatus)
            .Include(item => item.StatusHistory).ThenInclude(history => history.NewStatus)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        return Ok(ApiResponse<BrokerBookingDetailResponse>.Ok(ToDetail(booking)));
    }

    [HttpPatch("bookings/{id:long}/status")]
    public async Task<IActionResult> ChangeBookingStatus(
        long id,
        ChangeBrokerBookingStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.StatusCode))
        {
            return BadRequest(ApiResponse<object>.Fail("Status code is required."));
        }

        var booking = await ScopeBookings(_db.Bookings)
            .Include(item => item.Status)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        var targetCode = request.StatusCode.Trim().ToLowerInvariant();
        var targetStatus = await _db.BookingStatuses.FirstOrDefaultAsync(
            status => status.Code == targetCode && status.IsActive,
            cancellationToken);

        if (targetStatus is null)
        {
            return BadRequest(ApiResponse<object>.Fail("Booking status is not available."));
        }

        var currentCode = booking.Status?.Code ?? string.Empty;
        if (!AllowedTransitions.TryGetValue(currentCode, out var targets) ||
            !targets.Contains(targetCode, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<object>.Fail($"Status transition from '{currentCode}' to '{targetCode}' is not allowed."));
        }

        var oldStatusId = booking.StatusId;
        booking.StatusId = targetStatus.Id;
        booking.UpdatedByUserId = User.GetUserId();
        _db.BookingStatusHistory.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            OldStatusId = oldStatusId,
            NewStatusId = targetStatus.Id,
            ChangedByUserId = User.GetUserId(),
            Note = TextRules.CleanOptional(request.Note)
        });

        await _db.SaveChangesAsync(cancellationToken);
        var response = new BrokerBookingStatusChangeResponse(booking.Id, targetStatus.Code, targetStatus.Name);
        return Ok(ApiResponse<BrokerBookingStatusChangeResponse>.Ok(response));
    }

    private IQueryable<RentalHome> ScopeHomes(IQueryable<RentalHome> query)
    {
        if (User.IsAdmin()) return query;
        var userId = User.GetUserId();
        return query.Where(home => home.BrokerUserId == userId);
    }

    private IQueryable<Booking> ScopeBookings(IQueryable<Booking> query)
    {
        if (User.IsAdmin()) return query;
        var userId = User.GetUserId();
        return query.Where(booking => booking.RentalHome!.BrokerUserId == userId);
    }

    private static BrokerBookingListItemResponse ToListItem(Booking booking)
    {
        var dates = booking.Dates.OrderBy(date => date.Date).Select(date => date.Date).ToList();
        return new BrokerBookingListItemResponse(
            booking.Id,
            booking.RentalHomeId,
            booking.RentalHome?.Title ?? string.Empty,
            booking.CustomerFullName,
            booking.CustomerPhoneNumber,
            booking.Status?.Code ?? string.Empty,
            booking.Status?.Name ?? string.Empty,
            booking.TotalAmount,
            dates.Count,
            dates.Count == 0 ? null : dates[0],
            dates.Count == 0 ? null : dates[^1],
            booking.CreatedAt,
            booking.CustomerNote,
            booking.Status?.Code == BookingStatusCodes.WaitingDeposit);
    }

    private static BrokerBookingDetailResponse ToDetail(Booking booking)
    {
        var history = booking.StatusHistory
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new BrokerBookingStatusHistoryResponse(
                item.OldStatus?.Code,
                item.NewStatus?.Code ?? string.Empty,
                item.Note,
                item.CreatedAt))
            .ToList();
        var deposit = booking.Deposit is null ? null : DepositResponse.FromEntity(booking.Deposit);

        return new BrokerBookingDetailResponse(
            booking.Id,
            new BrokerBookingHomeResponse(
                booking.RentalHomeId,
                booking.RentalHome?.Title ?? string.Empty,
                booking.RentalHome?.City ?? string.Empty,
                booking.RentalHome?.District),
            new BrokerBookingCustomerResponse(booking.CustomerFullName, booking.CustomerPhoneNumber),
            new BrokerBookingStatusResponse(booking.Status?.Code ?? string.Empty, booking.Status?.Name ?? string.Empty),
            booking.DailyPrice,
            booking.TotalAmount,
            booking.Dates.OrderBy(item => item.Date).Select(item => item.Date).ToList(),
            booking.GuestCount,
            booking.CustomerNote,
            booking.CreatedAt,
            history,
            deposit);
    }
}
