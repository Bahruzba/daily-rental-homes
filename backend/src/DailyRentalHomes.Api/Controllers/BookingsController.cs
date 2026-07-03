using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Bookings;
using DailyRentalHomes.Api.Security;
using DailyRentalHomes.Api.Services;
using DailyRentalHomes.Domain.Constants;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly INotificationOutboxService _notifications;

    public BookingsController(AppDbContext db, INotificationOutboxService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var query = _db.Bookings.AsNoTracking();
        if (!User.IsAdmin())
        {
            var userId = User.GetUserId();
            query = query.Where(x => x.RentalHome!.BrokerUserId == userId);
        }

        var items = await query.OrderByDescending(x => x.Id).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }

    [Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var query = _db.Bookings
            .AsNoTracking()
            .Include(x => x.Dates)
            .Include(x => x.Deposit)
            .AsQueryable();

        if (!User.IsAdmin())
        {
            var userId = User.GetUserId();
            query = query.Where(x => x.RentalHome!.BrokerUserId == userId);
        }

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return item is null ? NotFound(ApiResponse<object>.Fail("Booking not found.")) : Ok(ApiResponse<object>.Ok(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewBookingRequest request, CancellationToken cancellationToken)
    {
        if (request.RentalHomeId <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Rental home is required."));
        }

        if (TextRules.Empty(request.Name) || request.Name.Trim().Length > 150)
        {
            return BadRequest(ApiResponse<object>.Fail("Customer name is required and must not exceed 150 characters."));
        }

        if (TextRules.Empty(request.Phone) || request.Phone.Trim().Length > 30)
        {
            return BadRequest(ApiResponse<object>.Fail("Phone is required and must not exceed 30 characters."));
        }

        if (request.Guests <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Guest count must be greater than zero."));
        }

        if (request.Dates is null || request.Dates.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("At least one booking date is required."));
        }

        if (request.Dates.Distinct().Count() != request.Dates.Count)
        {
            return BadRequest(ApiResponse<object>.Fail("Duplicate booking dates are not allowed."));
        }

        var dates = request.Dates.OrderBy(date => date).ToList();
        var rentalHome = await _db.RentalHomes
            .AsNoTracking()
            .FirstOrDefaultAsync(home => home.Id == request.RentalHomeId && !home.IsDeleted, cancellationToken);

        if (rentalHome is null)
        {
            return NotFound(ApiResponse<object>.Fail("Rental home not found."));
        }

        if (rentalHome.DailyPrice <= 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Rental home does not have a valid daily price."));
        }

        if (request.Guests > rentalHome.GuestCount)
        {
            return BadRequest(ApiResponse<object>.Fail($"Guest count cannot exceed {rentalHome.GuestCount}."));
        }

        var pendingStatus = await _db.BookingStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                status => status.Code == BookingStatusCodes.Pending && status.IsActive && !status.IsDeleted,
                cancellationToken);

        if (pendingStatus is null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse<object>.Fail("Pending booking status is not configured."));
        }

        var nonBlockingStatusCodes = new[] { BookingStatusCodes.Cancelled, BookingStatusCodes.Rejected };
        var conflictingDates = await _db.BookingDates
            .AsNoTracking()
            .Where(bookingDate =>
                !bookingDate.IsDeleted &&
                dates.Contains(bookingDate.Date) &&
                bookingDate.Booking != null &&
                !bookingDate.Booking.IsDeleted &&
                bookingDate.Booking.RentalHomeId == request.RentalHomeId &&
                bookingDate.Booking.Status != null &&
                !nonBlockingStatusCodes.Contains(bookingDate.Booking.Status.Code))
            .Select(bookingDate => bookingDate.Date)
            .Distinct()
            .OrderBy(date => date)
            .ToListAsync(cancellationToken);

        if (conflictingDates.Count > 0)
        {
            return BadRequest(ApiResponse<object>.Fail(
                $"Booking date conflict: {string.Join(", ", conflictingDates.Select(date => date.ToString("yyyy-MM-dd")))}."));
        }

        var customerUserId = await _db.Users
            .AsNoTracking()
            .Where(user => user.PhoneNumber == TextRules.Clean(request.Phone) && user.IsActive)
            .Select(user => (long?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var booking = new Booking
        {
            RentalHomeId = request.RentalHomeId,
            CustomerUserId = customerUserId,
            CustomerFullName = TextRules.Clean(request.Name),
            CustomerPhoneNumber = TextRules.Clean(request.Phone),
            GuestCount = request.Guests,
            DailyPrice = rentalHome.DailyPrice,
            TotalAmount = rentalHome.DailyPrice * dates.Count,
            StatusId = pendingStatus.Id,
            CustomerNote = TextRules.CleanOptional(request.Note)
        };

        foreach (var date in dates)
        {
            booking.Dates.Add(new BookingDate { Date = date });
        }

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);
        await _notifications.QueueBookingCreatedAsync(booking, rentalHome, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new BookingCreatedResponse(
            booking.Id,
            booking.RentalHomeId,
            pendingStatus.Code,
            pendingStatus.Name,
            booking.DailyPrice,
            booking.TotalAmount,
            dates,
            booking.CustomerFullName,
            booking.CustomerPhoneNumber,
            booking.CreatedAt);

        return Ok(ApiResponse<BookingCreatedResponse>.Ok(response));
    }

    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, ChangeBookingStatusRequest request, CancellationToken cancellationToken)
    {
        var query = _db.Bookings.AsQueryable();
        if (!User.IsAdmin())
        {
            var userId = User.GetUserId();
            query = query.Where(x => x.RentalHome!.BrokerUserId == userId);
        }

        var booking = await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (booking is null)
        {
            return NotFound(ApiResponse<object>.Fail("Booking not found."));
        }

        var oldStatusId = booking.StatusId;
        booking.StatusId = request.NewStatusId;
        var newStatusCode = await _db.BookingStatuses.AsNoTracking()
            .Where(status => status.Id == request.NewStatusId)
            .Select(status => status.Code)
            .FirstOrDefaultAsync(cancellationToken) ?? request.NewStatusId.ToString();

        _db.BookingStatusHistory.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            OldStatusId = oldStatusId,
            NewStatusId = request.NewStatusId,
            ChangedByUserId = User.GetUserId(),
            Note = TextRules.CleanOptional(request.Note)
        });

        await _notifications.QueueBookingStatusChangedAsync(booking, newStatusCode, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { booking.Id, booking.StatusId }));
    }
}
