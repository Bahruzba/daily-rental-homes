using DailyRentalHomes.Api.Common;
using DailyRentalHomes.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DailyRentalHomes.Api.Controllers;

[ApiController]
[Route("api/booking-statuses")]
public sealed class BookingStatusesController : ControllerBase
{
    private readonly AppDbContext _db;

    public BookingStatusesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var items = await _db.BookingStatuses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(items));
    }
}
