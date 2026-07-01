using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Api.Contracts.Bookings;
using DailyRentalHomes.Api.Security;
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

    public BookingsController(AppDbContext db)
    {
        _db = db;
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
        if (TextRules.Empty(request.Name) || TextRules.Empty(request.Phone) || request.Price <= 0 || request.Dates.Count == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Name, phone, price and dates are required."));
        }

        var booking = new Booking
        {
            RentalHomeId = request.RentalHomeId,
            CustomerFullName = TextRules.Clean(request.Name),
            CustomerPhoneNumber = TextRules.Clean(request.Phone),
            GuestCount = request.Guests,
            DailyPrice = request.Price,
            TotalAmount = request.Price * request.Dates.Count,
            StatusId = 1,
            CustomerNote = TextRules.CleanOptional(request.Note)
        };

        foreach (var item in request.Dates.Distinct())
        {
            booking.Dates.Add(new BookingDate { Date = item });
        }

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse<object>.Ok(new { booking.Id }));
    }

    [Authorize(Policy = AuthorizationPolicies.BrokerOrAdmin)]
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

        _db.BookingStatusHistory.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            OldStatusId = oldStatusId,
            NewStatusId = request.NewStatusId,
            ChangedByUserId = User.GetUserId(),
            Note = TextRules.CleanOptional(request.Note)
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { booking.Id, booking.StatusId }));
    }
}
