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
}
