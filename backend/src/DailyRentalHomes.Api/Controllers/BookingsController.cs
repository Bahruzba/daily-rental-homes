using DailyRentalHomes.Api.Contracts.Bookings;
using DailyRentalHomes.Domain.Entities;
using DailyRentalHomes.Infrastructure.Persistence;
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

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.Bookings.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var item = await _db.Bookings
            .AsNoTracking()
            .Include(x => x.Dates)
            .Include(x => x.Deposit)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(NewBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = new Booking
        {
            RentalHomeId = request.RentalHomeId,
            CustomerFullName = request.Name,
            CustomerPhoneNumber = request.Phone,
            GuestCount = request.Guests,
            DailyPrice = request.Price,
            TotalAmount = request.Price * request.Dates.Count,
            StatusId = 1,
            CustomerNote = request.Note
        };

        foreach (var item in request.Dates)
        {
            booking.Dates.Add(new BookingDate { Date = item });
        }

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(booking.Id);
    }

    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, ChangeBookingStatusRequest request, CancellationToken cancellationToken)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (booking is null)
        {
            return NotFound();
        }

        var oldStatusId = booking.StatusId;
        booking.StatusId = request.NewStatusId;

        _db.BookingStatusHistory.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            OldStatusId = oldStatusId,
            NewStatusId = request.NewStatusId,
            ChangedByUserId = request.ChangedByUserId,
            Note = request.Note
        });

        await _db.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}
